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
	using DevExpress.XtraEditors;
	using DevExpress.XtraEditors.Container;
	using DevExpress.XtraEditors.Repository;
	using DevExpress.XtraGrid;
	using DevExpress.XtraGrid.Views.Base;
	using DevExpress.XtraGrid.Views.Grid;
	using evt;
	using System.Collections;
	using System.Data;
	using System.Reflection;




	public class GridBindingSource : BindingSource, IDisposable, IDataBindingSource
	{
        private static manager.ILogger _logger =  manager.SLogManager.getInstance().getClassLogger(typeof(GridBindingSource));

		private IEditorsHost _editorsHost = null;
        private IGridAdapter _target = null;
        private Type  _dataType = null;
		

		private Dictionary<string, IList<CustomAttribute>> _attributesCache = new Dictionary<string, IList<CustomAttribute>>();
		private Dictionary<string, RepositoryItem> _repositories = new Dictionary<string, RepositoryItem>();
        private HashSet<string> _validableFields = null;
		private bool _gridIsConnected = false;

		//For type DataTable;
		private bool _isTypeDataTable = false;

        // hold last position before position changed occurs
        private int _oldPosition = -1;


		public Dictionary<string, IList<CustomAttribute>> AttributesCache { get { return _attributesCache; } }

		//if true it will handle eventual column text override
		public bool HandleCustomColumnDisplayText { get; set; } = false;


		public GridBindingSource() : this((IEditorsHost)null) { }
		public GridBindingSource(IContainer c) : this(null, c) { }
		public GridBindingSource(object o, string s) : this(null, o, s) { }
		public GridBindingSource(IEditorsHost eh) : base() { start(eh); }
		public GridBindingSource(IEditorsHost eh, IContainer c) : base(c){ start(eh); }
		public GridBindingSource(IEditorsHost eh, object o, string s) : base(o, s){ start(eh); }

		private void start(IEditorsHost eh)
		{
			_editorsHost = eh;
			if (_editorsHost != null && _editorsHost.FormSupport != null)
			{
				_editorsHost.FormSupport.AddBindingSource(this);
			}
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
					if (_target != null)
					{
                        _target.DataSourceChanged -= GridDataSourceChanged;
						_target.CustomRowCellEditForEditing -= CustomRowCellEditForEditingHandler;
                        _target.ShownEditor -= EditorShownHandler;
                        _target.CustomColumnDisplayText -= CustomColumnDisplayText; //try remove anyway
                        _target.ListSourceChanged -= DataController_ListSourceChanged;
                        _target.CellValueChanged -= _target_CellValueChanged;

                        //remove eventual grid cells editor repositories from grid
                        foreach (RepositoryItem ri in _repositories.Values)
						{
                            _target.RepositoryItems.Remove(ri);
						}
                        _target = null;
					}
					if (DataSource != null)
					{
						DataSource = null;
					}
					//clear cached attributes and repositories
					resetAttributes();

					ListChanged -= handleListItemChanged;
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
				//call inherited
				base.Dispose(disposing);
			}
		}
		#endregion


        public Type DataType
        {
            get
            {
                return _dataType;
            }
			set
			{
				_dataType = value;
			}
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
                if (value == null)
                {
                    // just empty grid rows
                    if(!ReferenceEquals(null, base.DataSource))
                    {
                        base.DataSource = null;
                    }

                    // if grid was not connected , manage remove useless columns
                    // we remove default columns, or columns present ingrid
                    if (!_gridIsConnected)
                    {
                        if (!ReferenceEquals(null, _target))
                        {
                            _target.ClearColumns();
                        }
                    }

                    return;
                }

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
					if (tmpDs is IList)
					{
						//try to obtain element type
						t = (tmpDs as IList).GetType().GetGenericArguments()[0];
					}
					else if (tmpDs is IEnumerable)
					{
						//try to obtain element type
						t = (tmpDs as IEnumerable).GetType().GetGenericArguments()[0];
					}
					else if (tmpDs is DataTable)
					{
						//Do nothing because type is set by _datatype setter						
						_isTypeDataTable = true;
					}
					else if (tmpDs is IListSource)
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

                // now when we know type we reset old handler
                if (!ReferenceEquals(null, _dataType))
                {
                    ListChanged -= handleListItemChanged;
                }

                // make generic Structure watch basing on type of DataSource element
                _oldPosition = -1;

				// _dataType musty be set before DS cause event can come in not correct order!!!!
				// we use timer on form with force message queue execution
				if (!ReferenceEquals(null, t))
				{
					_dataType = t;

                    // here we inspect type for obtain columns with validation
                    _validableFields = _dataType.GetPropertyNamesWithAttribute(typeof(attributes.CheckValidAttribute));

                }
                ForceInitializeGrid();
				base.DataSource = value;
				

                // now when we know type we set new handler
                if(!ReferenceEquals(null, _dataType))
                {
                    ListChanged += handleListItemChanged;
                }            

				//teree missing datacontroler so 
                if(_target is TreeListAdapter)
                {
                    ConnectGrid();
                }
			}
		}

        public void ForceInitializeGrid()
        {
            if (_target != null && !_target.IsReady)
            {
				_target.ForceInitialize(); // we need grid to initialize (it should be set in invisible component)
			}
        }

		public void AttachToGrid(GridControl g) {

#if DEBUG_TRACE_LOG_ON
			_logger.Debug("Set-GRID : New");
#endif
			//first disconnect eventual old one
			if (_target != null)
			{
                _target.DataSourceChanged -= GridDataSourceChanged;
				_target.CustomRowCellEditForEditing -= CustomRowCellEditForEditingHandler;
                _target.ShownEditor -= EditorShownHandler;
                _target.CustomColumnDisplayText -= CustomColumnDisplayText;
                _target.ListSourceChanged -= DataController_ListSourceChanged;
                _target.CellValueChanged -= _target_CellValueChanged;
                _target.ValidatingEditor -= _target_ValidatingEditor;
            }
            _target = new GridAdapter(g);
            _target.AutoPopulateColumns = true;
            _target.ListSourceChanged += DataController_ListSourceChanged;
            _target.DataSourceChanged += GridDataSourceChanged;
            _target.ValidatingEditor += _target_ValidatingEditor;
			//connect
			_target.DataSource = this;
        }

        
        public void AttachToTree(DevExpress.XtraTreeList.TreeList tree)
		{


#if DEBUG_TRACE_LOG_ON
			_logger.Debug("Set-GRID : New");
#endif
			//first disconnect eventual old one
			if (_target != null)
			{
				_target.DataSourceChanged -= GridDataSourceChanged;
				_target.CustomRowCellEditForEditing -= CustomRowCellEditForEditingHandler;
				_target.ShownEditor -= EditorShownHandler;
				_target.CustomColumnDisplayText -= CustomColumnDisplayText;
				_target.ListSourceChanged -= DataController_ListSourceChanged;
                _target.CellValueChanged -= _target_CellValueChanged;
                _target.ValidatingEditor -= _target_ValidatingEditor;
            }
			_target = new TreeListAdapter(tree);
			_target.AutoPopulateColumns = true;
			_target.ListSourceChanged += DataController_ListSourceChanged;
			_target.DataSourceChanged += GridDataSourceChanged;
            _target.ValidatingEditor += _target_ValidatingEditor;
            //connect
            _target.DataSource = this;
            _target.PopulateColumns();
        }


		private void DataController_ListSourceChanged(object sender, EventArgs e)
		{
			ConnectGrid();

            if (!ReferenceEquals(null, _editorsHost) && !ReferenceEquals(null, _dataType) && !ReferenceEquals(null, _target))
            {
                _editorsHost.onGridConnected(this, new GridConnectedEventData() { Control = _target, DataBindingSource = this, DataType = _dataType, Kind = GridConnectedEventKind.GridDataChanged });
            }
        }
		private void GridDataSourceChanged(object sender, EventArgs e)
		{
			ConnectGrid();

            if (!ReferenceEquals(null, _editorsHost) && !ReferenceEquals(null, _dataType) && !ReferenceEquals(null, _target))
            {
                _editorsHost.onGridConnected(this, new GridConnectedEventData() { Control = _target, DataBindingSource = this, DataType = _dataType, Kind = GridConnectedEventKind.GridDataChanged });
            }
        }

		public object addNewRecord(object rec)
		{
            object added = AddNew();
            added.CopyFrom(rec);
            return added;
		}

        public void setCurrentRecord(object rec)
		{
			Current.CopyFrom(rec);
		}


        //private void applyAttributes(PropertyInfo pi)
        private void applyAttributes(string propertyName)
        {
            IColumnAdapter gc = _target.ColumnByFieldName(propertyName);
            if (ReferenceEquals(null, gc)) return;
            // take all first, but cache just custom, but we need Standard Display 
            IEnumerable<Attribute> attrs = ReflectionHelper.GetAttributesFromPath(_dataType, propertyName);
            IList<CustomAttribute> ac = new List<CustomAttribute>();
            foreach (Attribute a in attrs)
            {


                if (a is CustomAttribute)
                {
                    GridColumnPopulated gcp = new GridColumnPopulated { FieldName = propertyName, RepositoryItem = null, Column = gc };
                    (a as CustomAttribute).applyGridColumnPopulation(this, gcp);
                    ac.Add(a as CustomAttribute);
                    RepositoryItem ri = gcp.RepositoryItem;
                    if (ri != null)
                    {
                        _target.RepositoryItems.Add(ri);
                        _repositories[propertyName] = ri;
                        if (gc != null) gc.ColumnEdit = ri;
                    }
                }
                if (a is xwcs.core.db.binding.attributes.StyleAttribute)
                {
                    xwcs.core.db.binding.attributes.StyleAttribute sa = a as xwcs.core.db.binding.attributes.StyleAttribute;
                    if (sa.VAlignment!= VAlignment.Default)
                    {
                        gc.VAlignment = sa.VAlignment;
                    }
                    if (sa.HAlignment!= HAlignment.Default)
                    {
                        gc.HAlignment = sa.HAlignment;
                    }

                    if (!ReferenceEquals(sa.ColumnWidth, null))
                    {
                        if (sa.ColumnWidth >= 0)
                        {
                            gc.Width = sa.ColumnWidth;
                        }
                    }
                    if (sa.BackgrounColor>0)
                    {
                        gc.BackGrndColor = sa.BackgrounColor;
                    }
                }

                // handle column name
                if (a is System.ComponentModel.DataAnnotations.DisplayAttribute)
                {
                    System.ComponentModel.DataAnnotations.DisplayAttribute da = a as System.ComponentModel.DataAnnotations.DisplayAttribute;

                    gc.Caption = da.GetName() ?? gc.Caption;
                    gc.Caption = da.GetShortName() ?? gc.Caption;
                    gc.VisibleIndex = da.GetOrder() ?? gc.VisibleIndex;

                    if (gc.Caption.ToUpper() != (da.GetName() ?? gc.Caption).ToUpper())
                    {
                        gc.ToolTip = da.GetName();

                    }
                    if (gc.ToolTip.ToUpper() != (da.GetDescription() ?? gc.ToolTip).ToUpper())
                    {
                        if (gc.ToolTip != "") { gc.ToolTip = gc.ToolTip + "\n"; }
                        gc.ToolTip = gc.ToolTip + da.GetDescription();
                    }
                }
            }

            if (ac.Count > 0)
                _attributesCache[propertyName] = ac;
           
            
            
            
		}
        
		private void ConnectGrid() 
		{
			if (_target != null && !_gridIsConnected && _dataType != null && _target.IsReady)
			{
				// check columns loaded
				if (_target.ColumnsCount() == 0)
				{
					_target.PopulateColumns();


				}

				if (!_isTypeDataTable)
				{
					PropertyInfo[] pis = _dataType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
					foreach (PropertyInfo pi in pis)
					{
						applyAttributes(pi.Name);
					}
				}
				else 
				{
					DataTable dt = base.DataSource as DataTable;
					if (dt != null)
					{
                        /*
						PropertyInfo[] pis = _dataType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
						foreach (PropertyInfo pi in pis)
						{
                            string strName = pi.Name;
							if (dt.Columns[strName]	!= null)
							{
								applyAttributes(pi.Name);
							}
						}
                        */
                        foreach (DataColumn c in dt.Columns)
                        {
                            
                            applyAttributes(c.ColumnName);
                        }
					}
				}
				//attach CustomRowCellEditForEditing event too
				_target.CustomRowCellEditForEditing += CustomRowCellEditForEditingHandler;
                _target.ShownEditor += EditorShownHandler;
				

				_target.CellValueChanged += _target_CellValueChanged;
				

				if (HandleCustomColumnDisplayText)
                    _target.CustomColumnDisplayText += CustomColumnDisplayText;
				
				_gridIsConnected = true;


                if (!ReferenceEquals(null, _editorsHost) && !ReferenceEquals(null, _dataType) && !ReferenceEquals(null, _target))
                {
                    _editorsHost.onGridConnected(this, new GridConnectedEventData() { Control = _target, DataBindingSource = this, DataType = _dataType, Kind = GridConnectedEventKind.GridConnected });
                }

                // probably good place save default layout
                _target.SaveLayout(LayoutDescriptor.makeDirectDefaultForType(_dataType));
            }
        }

		private void _target_CellValueChanged(object sender, CellValueChangedEventArgs e)
		{
			_target.PostChanges();
		}

		protected virtual void GetFieldDisplayText(object sender, CustomColumnDisplayTextEventArgs e) {
			//just to be overloaded if necessary	
		}


        // value validation
        private void _target_ValidatingEditor(object sender, DevExpress.XtraEditors.Controls.BaseContainerValidateEditorEventArgs e)
        {
            if (ReferenceEquals(null, _validableFields) || _validableFields.Count == 0) return;

            string cn = "";
            if (sender is GridView && Current is IValidableEntity)
            {
                cn = (sender as GridView).FocusedColumn.FieldName;
            }else if(sender is DevExpress.XtraTreeList.TreeList && Current is IValidableEntity)
            {
                cn = (sender as DevExpress.XtraTreeList.TreeList).FocusedColumn.FieldName;
            }

            if (cn != "" && _validableFields.Contains(cn))
            {
                Problem pr = (Current as IValidableEntity).ValidateProperty(cn, e.Value);
                if (pr.Kind != ProblemKind.None)
                {
                    e.Valid = false;
                    e.ErrorText = pr.ErrorMessage;
                }
            }
        }

        private void CustomColumnDisplayText(object sender, CustomColumnDisplayTextEventArgs e){

            if (_attributesCache.ContainsKey(e.Column.FieldName))
            {
                foreach (CustomAttribute a in _attributesCache[e.Column.FieldName])
                {
                    a.applyGetFieldDisplayText(this, e);
                }
            }

            // eventual override
            GetFieldDisplayText(sender, e);
        }

		private void EditorShownHandler(object sender, EventArgs e) 
		{
			ColumnView view;
			DevExpress.XtraTreeList.TreeList treeList;
            ViewEditorShownEventArgs vsea = null;

            if ((view = sender as ColumnView) != null)
			{
				vsea = new ViewEditorShownEventArgs
				{
					Control = view.ActiveEditor,
					View = view,
					FieldName = view.FocusedColumn.FieldName,
					RepositoryItem = ((BaseEdit)view.ActiveEditor).Properties
                 };						
			}
			else	
			if ((treeList = sender as DevExpress.XtraTreeList.TreeList) != null)
			{
				vsea = new ViewEditorShownEventArgs
				{
					Control = treeList.ActiveEditor,
					TreeList = treeList,
					FieldName = treeList.FocusedColumn.FieldName,
					RepositoryItem = ((BaseEdit)treeList.ActiveEditor).Properties
                };
			}

            if (vsea != null && _attributesCache.ContainsKey(vsea.FieldName))
            {
                foreach (CustomAttribute a in _attributesCache[vsea.FieldName])
                {
                    a.applyCustomEditShown(this, vsea);
                }
            }
        }

		private void CustomRowCellEditForEditingHandler(object sender, CustomRowCellEditEventArgs e) 
		{
			if (_repositories.ContainsKey((e.GridLike?e.Column.FieldName:e.TreeColumn.FieldName)))
			{
				e.RepositoryItem = _repositories[(e.GridLike ? e.Column.FieldName : e.TreeColumn.FieldName)];
			}
			if (_attributesCache.ContainsKey((e.GridLike ? e.Column.FieldName : e.TreeColumn.FieldName)))
			{
				foreach (CustomAttribute a in _attributesCache[(e.GridLike ? e.Column.FieldName : e.TreeColumn.FieldName)])
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


		protected virtual void resetSlavesOfModifiedProperty(ResetSlavesAttribute att) {
			//should be overridden

		}

		private void handleListItemChanged(object sender, ListChangedEventArgs args)
		{
			if(args.ListChangedType == ListChangedType.ItemChanged)
			{
				if(args.PropertyDescriptor != null) {
                    try
                    {
                        IEnumerable<CustomAttribute> attrs = ReflectionHelper.GetCustomAttributesFromPath(_dataType, args.PropertyDescriptor.Name);
                        var ra = attrs.OfType<ResetSlavesAttribute>();
                        if (ra.Count() == 1)
                        {
                            //we need reset slaves chain
                            resetSlavesOfModifiedProperty(ra.First());
                        }

#if DEBUG_TRACE_LOG_ON
					                        _logger.Debug("Item changed! " + args.PropertyDescriptor.Name);
#endif
                    } catch (ArgumentOutOfRangeException)
                    {
                        
                    }
				}
			}
		}

        public Control GetControlByModelProperty(string ModelropertyName)
        {
            return null;
        }

        public void SuspendLayout()
        {
            return;
        }

        public void ResumeLayout()
        {
            return;
        }


        protected override void OnPositionChanged(EventArgs e)
        {
            if (_oldPosition == Position) return; // skip call on the same line
            base.OnPositionChanged(e);
            //store current in old
            _oldPosition = Position; // this means first one will not exist!!!
        }

        public bool ChangeLayout(string LayoutSuffix)
        {
            throw new NotImplementedException();
        }

        public int LastPosition
        {
            get
            {
                return _oldPosition;
            }
        }

        // grid layout management
        public void SaveGridLayout()
        {
            if(_dataType != null)
            {
                _target.SaveLayout(LayoutDescriptor.makeDirectForType(_dataType));
            }
        }

        public void LoadGridLayout(LayoutDescriptor descr)
        {
            if (_dataType != null)
            {
                _target.LoadLayout(descr);
            }
        }

        public void LoadGridLayout()
        {
            LoadGridLayout(LayoutDescriptor.makeDirectForType(_dataType));
        }

        public void RestoreDefaultGridLayout()
        {
            if(_dataType != null)
            {
                _target.LoadLayout(LayoutDescriptor.makeDirectDefaultForType(_dataType));
            }
        }
    }
}
