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
	using DevExpress.XtraGrid;
	using DevExpress.XtraLayout;
	using evt;
	using manager;
	using System.Collections;
	using System.Data;
	using System.IO;
	using System.Reflection;
	using ui.db;

	public class DataLayoutBindingSource : BindingSource, IDataBindingSource, INotifyModelPropertyChanged, INotifyCurrentObjectChanged, IDisposable
    {
		protected static manager.ILogger _logger =  manager.SLogManager.getInstance().getClassLogger(typeof(DataLayoutBindingSource));

		// this variable will be propagated to all datasource chain down to the 
		// model, normaly this should be some form or other root container whih 
		// will resolve all combo box datasets
		private IEditorsHost _editorsHost = null;


		private DataLayoutControl _cnt = null;
        private Control _contentContainer = null;
		private Type  _dataType;


        protected Dictionary<string, IList<CustomAttribute>> _attributesCache = new Dictionary<string, IList<CustomAttribute>>();
		private object _oldCurrent = null;
        protected bool _fieldsAreRetrieved = false;
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
            if(_editorsHost != null && _editorsHost.FormSupport != null)
            {
                _editorsHost.FormSupport.AddBindingSource(this);
            }
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
				if (!_fieldsAreRetrieved && !ReferenceEquals(null, _cnt))
				{
                    SuspendLayout();

                    _cnt.RetrieveFields();

                    // use file suffix if present <- used in intial loads
                    // so file suffix must be set prior ds settings

                    TryLoadLayuotFromFile(CurrentLayoutSuffix);

                    

                    if (_fieldsAreRetrieved)
                    {
                        // there should be registered all triggers
                        _wes_CurrentObjectChanged?.Raise(this, new CurrentObjectChangedEventArgs() { Old = _oldCurrent, Current = base.Current });
                    }

                    ResumeLayout();
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
#if DEBUG_TRACE_LOG_ON
				_logger.Debug("Set-DL : New");
#endif

				if (_cnt == value) return;
				//first disconnect eventual old one
				if (_cnt != null)
				{
					_cnt.FieldRetrieved -= FieldRetrievedHandler;
					_cnt.FieldRetrieving -= FieldRetrievingHandler;
				}
				if (value != null) {
					_cnt = value;

                    SuspendLayout();

                    _cnt.AllowGeneratingNestedGroups = DevExpress.Utils.DefaultBoolean.True;
                    _cnt.AllowGeneratingCollectionProperties = DevExpress.Utils.DefaultBoolean.False;
                    _cnt.AutoRetrieveFields = true;
#if DEBUG
                    _cnt.AllowCustomization = true;
#else
                    _cnt.AllowCustomization = false;
#endif
                    _cnt.OptionsItemText.TextAlignMode = DevExpress.XtraLayout.TextAlignMode.AlignInGroups;
                    _cnt.FieldRetrieved += FieldRetrievedHandler;
					_cnt.FieldRetrieving += FieldRetrievingHandler;
					//variables first
					_resetLayoutRequest = false;
					_fieldsAreRetrieved = false;

                   
                    //connect
                    _cnt.DataSource = this;


                    if (!_fieldsAreRetrieved && !ReferenceEquals(null, _cnt))
                    {
                        _cnt.RetrieveFields();

                        // use file suffix if present <- used in intial loads
                        // so file suffix must be set prior ds settings

                        TryLoadLayuotFromFile(CurrentLayoutSuffix);



                        if (_fieldsAreRetrieved)
                        {
                            // there should be registered all triggers
                            _wes_CurrentObjectChanged?.Raise(this, new CurrentObjectChangedEventArgs() { Old = _oldCurrent, Current = base.Current });
                        }

                    }
                    ResumeLayout();
                }
				else {
					_cnt = null;
				}				
			}
		}

        public string LayoutBaseFileName { get; set; } = "";
        public string CurrentLayoutSuffix { get; set; } = "";

#endregion

        public bool ChangeLayout(string LayoutSuffix)
        {
            if (CurrentLayoutSuffix.Equals(LayoutSuffix)) return false;

            if (LayoutSuffix.Length > 0)
            {
                CurrentLayoutSuffix = LayoutSuffix;
                if (!TryLoadLayuotFromFile(LayoutSuffix))
                {
                    CurrentLayoutSuffix = "_Default";
                    return TryLoadLayuotFromFile(CurrentLayoutSuffix);
                }
                return true;
            }
            else
            {
                if (CurrentLayoutSuffix.Equals("_Default")) return false;

                CurrentLayoutSuffix = "_Default";
                return TryLoadLayuotFromFile(CurrentLayoutSuffix);
            }           
        }

        /// <summary>
        ///  sets data layout container control
        ///  data layout control will be dynamic
        /// </summary>
    
        public Control LayoutContainer
        {
            get
            {
                return _contentContainer;
            }
            set
            {
                if(value != null && _contentContainer != value)
                {
                    _contentContainer = value;
                    MakeNewLayoutControl();
                }
            }
        }

        private void MakeNewLayoutControl()
        {
            // remove old
            if (!ReferenceEquals(null, _cnt)) {
                _contentContainer.Controls.Remove(_cnt);
            }            
            DataLayoutControl dl = new DataLayoutControl();
            _contentContainer.Controls.Add(dl);
            dl.Dock = DockStyle.Fill;
            dl.AllowGeneratingNestedGroups = DevExpress.Utils.DefaultBoolean.True;
            dl.AllowGeneratingCollectionProperties = DevExpress.Utils.DefaultBoolean.False;
            dl.AutoRetrieveFields = true;
            dl.AllowCustomization = true;
            dl.OptionsCustomizationForm.EnableUndoManager = true;
            dl.OptionsCustomizationForm.ShowPropertyGrid = true;
            dl.OptionsCustomizationForm.ShowRedoButton = true;
            //dl.OptionsCustomizationForm.ShowUndoButton = true;
            dl.OptionsItemText.TextAlignMode = DevExpress.XtraLayout.TextAlignMode.CustomSize;
            this.DataLayout = dl;
        }


        private bool TryLoadLayuotFromFile(string FileNameSuffix = "")
        {
            // check if connected to host
            if (
                ReferenceEquals(base.DataSource, null) || 
                ReferenceEquals(null, _editorsHost) ||
                LayoutBaseFileName.Length == 0 || 
                !SPersistenceManager.getInstance().IsAllowed_LoadLayoutFromXml) return false;

            

            string filePath = string.Format("{0}/{1}{2}.xml", _editorsHost.LayoutAssetsPath, LayoutBaseFileName, FileNameSuffix);
            if (File.Exists(filePath))
            {  
                _cnt.RestoreLayoutFromXml(filePath);
                //test for resising foont. Approz work, but not resize crid font
                //SetFontSize(_cnt, 10);
                return true;
            }

            _logger.Info("Missing layout file: " + filePath);
            return false;           
        }

        private void SetFontSize(Control cnt, float ems)
        {
            cnt.Font = new System.Drawing.Font(cnt.Font.FontFamily, ems);
            if (cnt is ContainerControl)
            {
                foreach (Control c in (cnt as ContainerControl).Controls)
                {
                    SetFontSize(c, ems);
                }
            }
            
            if (cnt.GetType().GetProperties().Where(p => p.Name == "ControlCollection").Any())
            {
                try
                {
                    var cc = cnt.GetPropertyByName("ControlCollection");
                    foreach (Control c in (cc as Control.ControlCollection))
                    {
                        SetFontSize(c, ems);
                    }
                } catch(Exception
#if DEBUG_TRACE_LOG_ON
                    ex){ _logger.Debug(ex.ToString());
#else
                ) { 
#endif
                }
            }
        }

        // we will hook model properties changed event
        private void CurrentItemPropertyChanged(object sender, ModelPropertyChangedEventArgs e)
        {
#if DEBUG_TRACE_LOG_ON
            _logger.Debug(string.Format("CC-Current Item Property: {0} changed", e));
#endif
            _wes_ModelPropertyChanged?.Raise(this, e);
        }

        private WeakEventSource<ModelPropertyChangedEventArgs> _wes_ModelPropertyChanged = null;
        public event EventHandler<ModelPropertyChangedEventArgs> ModelPropertyChanged
        {
            add
            {
                if (_wes_ModelPropertyChanged == null)
                {
                    _wes_ModelPropertyChanged = new WeakEventSource<ModelPropertyChangedEventArgs>();
                }
                _wes_ModelPropertyChanged.Subscribe(value);
            }
            remove
            {
                _wes_ModelPropertyChanged?.Unsubscribe(value);
            }
        }

        private WeakEventSource<CurrentObjectChangedEventArgs> _wes_CurrentObjectChanged = null;
        public event EventHandler<CurrentObjectChangedEventArgs> CurrentObjectChanged
        {
            add
            {
                if (_wes_CurrentObjectChanged == null)
                {
                    _wes_CurrentObjectChanged = new WeakEventSource<CurrentObjectChangedEventArgs>();
                }
                _wes_CurrentObjectChanged.Subscribe(value);
            }
            remove
            {
                _wes_CurrentObjectChanged?.Unsubscribe(value);
            }
        }

        // not used for this object type, but necessary for interface
        public bool IsChanged()
        {
            return false;
        }

        private void resetDataLayout() {
			// reset layout if
			// there is one active
			if (_cnt != null && DataSource != null && _fieldsAreRetrieved)
			{
                SuspendLayout();
                
#if DEBUG_TRACE_LOG_ON
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

                _cnt.RetrieveFields();

                // handle eventual layout loading here
                //TryLoadLayuotFromFile();

                ResumeLayout();
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
#if DEBUG_TRACE_LOG_ON
			_logger.Debug("CC-Current ["+sender+"] : " + (base.Current != null ? base.Current.GetPropValueByPathUsingReflection("id") : "null"));
#endif

			if (_oldCurrent != base.Current) {
				
				if(_structureWatcher != null) {
					//de-serialize if necessary
#if DEBUG_TRACE_LOG_ON
					_logger.Debug("CC-Current Deserialize");
#endif
					(base.Current as SerializedEntityBase).DeserializeFields();
					_resetLayoutRequest = _structureWatcher.CheckStructure(base.Current as SerializedEntityBase);
				}

                // handle hook to property changed event	
                if (!ReferenceEquals(null, _oldCurrent) && _oldCurrent is INotifyModelPropertyChanged)
                {
                    (_oldCurrent as INotifyModelPropertyChanged).ModelPropertyChanged -= CurrentItemPropertyChanged;
                }
                if (base.Current is INotifyModelPropertyChanged)
                {
                    (base.Current as INotifyModelPropertyChanged).ModelPropertyChanged += CurrentItemPropertyChanged;
                }

                // notify rest of new current object but just if layout is ready
                if (_fieldsAreRetrieved)
                {
                    _wes_CurrentObjectChanged?.Raise(this, new CurrentObjectChangedEventArgs() { Old = _oldCurrent, Current = base.Current });
                }                

                _oldCurrent = base.Current;                

                //if there is no more valid layout reset is
                if (_resetLayoutRequest)
				{
					resetDataLayout();
				}
			}
#if DEBUG_TRACE_LOG_ON
			_logger.Debug("CC-OUT-Current: " + (base.Current != null ? base.Current.GetPropValueByPathUsingReflection("id") : "null"));
#endif
		}

       
        virtual protected void FieldRetrievedHandler(object sender, FieldRetrievedEventArgs e)
		{
#if DEBUG_TRACE_LOG_ON
			_logger.Debug("Retrieving for field:" + e.FieldName);
#endif

            // force customisation form field name, can be overriden with Style.FormCustomisationCaption
            e.Item.CustomizationFormText = e.FieldName;

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
#if DEBUG_TIMING_LOG_ON
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

#if DEBUG_TIMING_LOG_ON
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

        // walk trough Datalayout component and find control  by Model property name      
        public Control GetControlByModelProperty(string ModelPropertyName)
        {
            if (ReferenceEquals(null, _cnt)) return null;

            return _cnt.Items.Where(i => i is LayoutControlItem).Cast<LayoutControlItem>()
            .Where(o => !ReferenceEquals(o.Control, null))
            .Where(o => o.Control.DataBindings.Count > 0 && o.Control.DataBindings[0].BindingMemberInfo.BindingMember == ModelPropertyName)
            .Select(o => o.Control)
            .FirstOrDefault();
        }

		public void SetReadOnly(bool bOn)
		{
			//List<Control> l1 =
			IFormSupport fs = _editorsHost.FormSupport;
			_cnt.Items.Where(i => i is LayoutControlItem).Cast<LayoutControlItem>()
				.Where(o => o.Control!=null && o.Control.DataBindings.Count > 0)
				.Select(o => o.Control)
				.ToList().ForEach(e => {
					if(bOn) {
						// read only for all
						if(e is BaseEdit) {
							(e as BaseEdit).ReadOnly = true;
						}
					}
					else {
						if (e is BaseEdit)
						{
							(e as BaseEdit).ReadOnly = !fs.ControlsMeta.ContainsKey((e as BaseEdit)) ?  false : fs.ControlsMeta[(e as BaseEdit)].ReadOnly;
						}
					}	
				});
		}

        public void SuspendLayout()
        {
			if (!ReferenceEquals(_cnt, null))
			{
				//_cnt.BeginUpdate();
				//_cnt.SuspendLayout();
			}
		}

        public void ResumeLayout()
        {
			if (!ReferenceEquals(_cnt, null))
			{
				//_cnt.ResumeLayout();
				//_cnt.EndUpdate();
			}
        }


        public string GetFieldName()
        {
            // not handled name so just send type
            return "DataLayoutBindingSource";
        }

        
    }
}