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
	using DevExpress.XtraGrid;
	using evt;
	using System.Collections;
	using System.Data;
	using System.Reflection;
	

	public class DataLayoutBindingSource : BindingSource, IDataBindingSource, IDisposable
	{
		private static manager.ILogger _logger =  manager.SLogManager.getInstance().getClassLogger(typeof(DataLayoutBindingSource));

		// this variable will be propagated to all datasource chain down to the 
		// model, normaly this should be some form or other root container whih 
		// will resolve all combo box datasets
		private IEditorsHost _editorsHost = null;


		private DataLayoutControl _cnt = null;
		private Type  _dataType;


		private Dictionary<string, IList<CustomAttribute>> _attributesCache = new Dictionary<string, IList<CustomAttribute>>();
		private object _oldCurrent = null;
		private bool _fieldsAreRetrieved = true;
		private bool _resetLayoutRequest = false;
		
		//if we work with serialized entities
		private StructureWatcher _structureWatcher = null;

		public DataLayoutBindingSource() : this((IEditorsHost)null) {}
		public DataLayoutBindingSource(IContainer c) : this(null, c) { }
		public DataLayoutBindingSource(object o, string s) : this(null, o, s) { }
		public DataLayoutBindingSource(IEditorsHost eh) : base() { start(eh); }
		public DataLayoutBindingSource(IEditorsHost eh, IContainer c) : base(c){ start(eh); }
		public DataLayoutBindingSource(IEditorsHost eh, object o, string s) : base(o, s){ start(eh); }
		
		private void start(IEditorsHost eh)
		{
			_editorsHost = eh;
			CurrentChanged += handleCurrentChanged;
		}

		#region IDisposable Support
		protected bool disposedValue = false; // To detect redundant calls
		protected override void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					//only if disposing is called from Dispose pattern
					CurrentChanged -= handleCurrentChanged;

					//disconnect events in any case
					if (_cnt != null)
					{
						_cnt.FieldRetrieved -= FieldRetrievedHandler;
						_cnt.FieldRetrieving -= FieldRetrievingHandler;
						_cnt = null;
					}

					resetAttributes();
					
					if (DataSource != null)
					{
						DataSource = null;
					}
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
				//call inherited
				base.Dispose(disposing);
			}
		}
		#endregion



		#region properties

		public Dictionary<string, IList<CustomAttribute>> AttributesCache { get { return _attributesCache; } }

		public IEditorsHost EditorsHost {
			get {
				return _editorsHost;
			}
		}

		public new object DataSource
		{
			get
			{
				return base.DataSource;
			}

			set
			{
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
							_logger.Error("Missing DataSource for data layout");
							return; // no valid binding arrived so we skip 
						}
					}
				}
				else
				{
					t = tmpT;
				}

				bool isSerialized = typeof(SerializedEntityBase).IsAssignableFrom(t);

				if (isSerialized)
				{

					if (_structureWatcher != null)
					{
						if (!_structureWatcher.IsCompatible(t))
						{
							_structureWatcher = new StructureWatcher(t);
						}
					}
					else
					{
						_structureWatcher = new StructureWatcher(t);
					}
				}
				else
				{
					_structureWatcher = null;
				}
				// make generic Structure watch basing on type of DataSource element
				base.DataSource = value;
				_dataType = t;

				// load fields eventually, layout should be assigned before
				// so we need do eventually also this
				if (!_fieldsAreRetrieved)
				{
					_cnt.RetrieveFields();
				}

			}
		}

		public DataLayoutControl DataLayout
		{
			get
			{
				return _cnt;
			}
			set
			{
#if DEBUG
				_logger.Debug("Set-DL : New");
#endif

				if (_cnt == value) return;
				//first disconnect eventual old one
				if (_cnt != null)
				{
					_cnt.FieldRetrieved -= FieldRetrievedHandler;
					_cnt.FieldRetrieving -= FieldRetrievingHandler;
				}
				_cnt = value;
				_cnt.AllowGeneratingNestedGroups = DevExpress.Utils.DefaultBoolean.True;
				_cnt.AutoRetrieveFields = true;
				_cnt.AllowCustomization = false;
				//_cnt.AllowCustomizationMenu = false;
				_cnt.FieldRetrieved += FieldRetrievedHandler;
				_cnt.FieldRetrieving += FieldRetrievingHandler;
				//variables first
				_resetLayoutRequest = false;
				_fieldsAreRetrieved = false;
				//connect
				_cnt.DataSource = this;
			}
		}

		#endregion

		private void resetDataLayout() {
			// reset layout if
			// there is one active
			if (_cnt != null && DataSource != null && _fieldsAreRetrieved)
			{
#if DEBUG
				if (CurrencyManager.Position >= 0)
					_logger.Debug("Reset layout");
#endif				
				_cnt.DataSource = null;
				_cnt.DataBindings.Clear();
				_cnt.Clear();
				_resetLayoutRequest = false;
				_fieldsAreRetrieved = false;
				resetAttributes();
				// now set new source
				_cnt.DataSource = this;
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

		

		private void handleCurrentChanged(object sender, object args)
		{
#if DEBUG
			_logger.Debug("CC-Current ["+sender+"] : " + (base.Current != null ? base.Current.GetPropValueByPathUsingReflection("id") : "null"));
#endif

			if (_oldCurrent != base.Current) {
				
				if(_structureWatcher != null) {
					//de-serialize if necessary
#if DEBUG
					_logger.Debug("CC-Current Deserialize");
#endif
					(base.Current as SerializedEntityBase).DeserializeFields();
					_resetLayoutRequest = _structureWatcher.CheckStructure(base.Current as SerializedEntityBase);
				}				

				_oldCurrent = base.Current;

				//if there is no more valid layout reset is
				if (_resetLayoutRequest)
				{
					resetDataLayout();
				}
			}
#if DEBUG
			_logger.Debug("CC-OUT-Current: " + (base.Current != null ? base.Current.GetPropValueByPathUsingReflection("id") : "null"));
#endif
		}

		

		private void FieldRetrievedHandler(object sender, FieldRetrievedEventArgs e)
		{
#if DEBUG
				_logger.Debug("Retrieving for field:" + e.FieldName);
#endif
				if (_attributesCache.ContainsKey(e.FieldName))
				{
					foreach (CustomAttribute a in _attributesCache[e.FieldName])
					{
						a.applyRetrievedAttribute(this, e);
					}
				}
				// at the end say that layout is valid
				// TODO: verify what happen if there is a change in the middle, this is called for each field separately
				_fieldsAreRetrieved = true;
		}

		private void FieldRetrievingHandler(object sender, FieldRetrievingEventArgs e)
		{
#if DEBUG
			Stopwatch sw = new Stopwatch();
			sw.Start();
#endif
			if (base.Current != null) {
				IEnumerable<CustomAttribute> attrs = ReflectionHelper.GetCustomAttributesFromPath(base.Current.GetType(), e.FieldName);
				IList<CustomAttribute> ac = new List<CustomAttribute>();
				foreach (CustomAttribute a in attrs)
				{
					a.applyRetrievingAttribute(this, e);
					ac.Add(a as CustomAttribute);
				}
				if(ac.Count > 0)
					_attributesCache[e.FieldName] = ac;
			}

#if DEBUG
			sw.Stop();
			_logger.Debug(String.Format("Elapsed={0}", sw.Elapsed));
#endif
			
		
			// fixed things
			e.DataSourceUpdateMode = DataSourceUpdateMode.OnPropertyChanged;
			e.Handled = true;
		}

		//if there is change or we dispose we need reset attributes
		private void resetAttributes() {
			_attributesCache.Values.ToList().ForEach(e => { e.ToList().ForEach(a => a.unbind(this)); e.Clear(); });
			_attributesCache.Clear();
		}
	}
}