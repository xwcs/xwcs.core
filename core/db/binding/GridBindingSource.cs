using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.ComponentModel;
using xwcs.core.db.model;
using System.Diagnostics;
using DevExpress.XtraDataLayout;

namespace xwcs.core.db.binding
{
	using attributes;
	using DevExpress.XtraEditors.Container;
	using DevExpress.XtraEditors.Repository;
	using DevExpress.XtraGrid;
	using DevExpress.XtraGrid.Views.Grid;
	using evt;
	using System.Collections;
	using System.Data;
	using System.Reflection;

	


	public class GridBindingSource : BindingSource, IDisposable, IDataBindingSource
	{
		private static manager.ILogger _logger =  manager.SLogManager.getInstance().getClassLogger(typeof(GridBindingSource));

		private IEditorsHost _editorsHost = null;
		private GridControl _grid = null; 
		private Type  _dataType = null;

		private Dictionary<string, IList<CustomAttribute>> _attributesCache = new Dictionary<string, IList<CustomAttribute>>();
		private Dictionary<string, RepositoryItem> _repositories = new Dictionary<string, RepositoryItem>();
		private bool _gridIsConnected = false;

		public GridBindingSource() : this((IEditorsHost)null) { }
		public GridBindingSource(IContainer c) : this(null, c) { }
		public GridBindingSource(object o, string s) : this(null, o, s) { }
		public GridBindingSource(IEditorsHost eh) : base() { start(eh); }
		public GridBindingSource(IEditorsHost eh, IContainer c) : base(c){ start(eh); }
		public GridBindingSource(IEditorsHost eh, object o, string s) : base(o, s){ start(eh); }

		private void start(IEditorsHost eh)
		{
			_editorsHost = eh;
		}

		public IEditorsHost EditorsHost
		{
			get
			{
				return _editorsHost;
			}
		}

		public new object DataSource {
			get {
				return base.DataSource; 
			}

			set {
				if (value == null) return;

				Type t = null;

				object tmpDs = null;

				//read annotations
				//here it depends what we have as DataSource, it can be Object, Type or IList Other we will ignore
				BindingSource bs = value as BindingSource;
				if (bs == null)
				{
					//no binding source
					tmpDs = value;
				}
				else
				{
					tmpDs = bs.DataSource;
				}

				Type tmpT = tmpDs as Type;
				if (tmpT == null)
				{
					//lets try another way, maybe IList
					if (tmpDs as IList != null)
					{
						//try to obtain element type
						t = (tmpDs as IList).GetType().GetGenericArguments()[0];
					}
					else if (tmpDs as IEnumerable != null)
					{
						//try to obtain element type
						t = (tmpDs as IEnumerable).GetType().GetGenericArguments()[0];
					}
					else if (tmpDs as IListSource != null)
					{
						//try to obtain element type
						t = (tmpDs as IListSource).GetType().GetGenericArguments()[0];
					}
					else
					{
						//it should be plain object and try to take type
						if ((tmpDs as object) != null)
						{
							t = tmpDs.GetType();
						}
						else
						{
							_logger.Error("Missing DataSource for grid layout");
							return; // no valid binding arrived so we skip 
						}
					}
				}
				else {
					t = tmpT;
				}

				// make generic Structure watch basing on type of DataSource element
				base.DataSource = value;
				_dataType = t;

				if (!_gridIsConnected) {
					ConnectGrid();	
				}
			}
		}

		public GridControl Grid {
			get {
				return _grid;
			}

			set {
#if DEBUG
				_logger.Debug("Set-GRID : New");
#endif
				if (_grid == value) return;
				//first disconnect eventual old one
				if (_grid != null)
				{
					_grid.DataSourceChanged -= GridDataSourceChanged;
					GridView gv = _grid.MainView as GridView;
					if (gv != null)
					{
						gv.CustomRowCellEditForEditing -= CustomRowCellEditForEditingHandler;
						gv.ShownEditor -= EditorShownHandler;
					}
				}
				_grid = value;
				_grid.DataSourceChanged += GridDataSourceChanged;
				//connect
				_grid.DataSource = this;
			}
		}

		public void addNewRecord(object rec)
		{
			AddNew();
			Current.CopyFrom(rec);
		}

		public void setCurrentRecord(object rec)
		{
			Current.CopyFrom(rec);
		}

		private void GridDataSourceChanged(object sender, EventArgs e)
		{
			_grid.MainView.PopulateColumns();
			ConnectGrid();
		}

		private void ConnectGrid() {
			if (_grid != null && !_gridIsConnected && _dataType != null)
			{
				PropertyInfo[] pis = _dataType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
				foreach (PropertyInfo pi in pis) {
					IEnumerable<CustomAttribute> attrs = ReflectionHelper.GetCustomAttributesFromPath(_dataType, pi.Name);
					IList<CustomAttribute> ac = new List<CustomAttribute>();
					foreach (CustomAttribute a in attrs)
					{
						RepositoryItem ri = a.applyGridColumnPopulation(this, pi.Name);
						ac.Add(a as CustomAttribute);
						if(ri != null) {
							_grid.RepositoryItems.Add(ri);
							_repositories[pi.Name] = ri;
						}
					}
					if (ac.Count > 0)
						_attributesCache[pi.Name] = ac;

				}
				//attach CustomRowCellEditForEditing event too
				GridView gv = _grid.MainView as GridView;
				if(gv!=null) {
					gv.CustomRowCellEditForEditing += CustomRowCellEditForEditingHandler;
					gv.ShownEditor += EditorShownHandler;
				}

				_gridIsConnected = true;
			}
		}

		

		

		private void EditorShownHandler(object sender, EventArgs e) {

		}

		private void CustomRowCellEditForEditingHandler(object sender, CustomRowCellEditEventArgs e) {
			if (_repositories.ContainsKey(e.Column.FieldName)) {
				e.RepositoryItem = _repositories[e.Column.FieldName];
			}
			if (_attributesCache.ContainsKey(e.Column.FieldName))
			{
				foreach (CustomAttribute a in _attributesCache[e.Column.FieldName])
				{
					a.applyCustomRowCellEdit(this, e);
				}
			}
		}

		//if there is change or we dispose we need reset attributes
		private void resetAttributes()
		{
			_attributesCache.Values.ToList().ForEach(e => { e.ToList().ForEach(a => a.unbind(this)); e.Clear(); });
			_attributesCache.Clear();
			//clear repositories cache	
			_repositories.Values.ToList().ForEach(r => r.Dispose());				
			_repositories.Clear();
		}

		#region IDisposable Support
		protected bool disposedValue = false; // To detect redundant calls
		protected override void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					//disconnect events in any case
					if(_grid != null) {
						_grid.DataSourceChanged -= GridDataSourceChanged;
						GridView gv = _grid.MainView as GridView;
						if (gv != null)
						{
							gv.CustomRowCellEditForEditing -= CustomRowCellEditForEditingHandler;
						}
						//remove eventual grid cells editor repositories from grid
						foreach (RepositoryItem ri in _repositories.Values)
						{
							_grid.RepositoryItems.Remove(ri);
						}
						_grid = null;
					}
					if(DataSource != null) {
						DataSource = null;
					}
					//clear cached attributes and repositories
					resetAttributes();					
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
				//call inherited
				base.Dispose(disposing);
			}
		}
		#endregion
	}
}
