using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.ComponentModel;
using xwcs.core.db.model;
using System.Diagnostics;
using DevExpress.XtraDataLayout;
using xwcs.core.db.model;

namespace xwcs.core.db.binding
{
	using attributes;
	using System.Collections;
	using System.Data;
	public class GetFieldQueryableEventData
	{
		public object DataSource { get; set; }
		public string FieldName { get; set; }
	}

    public class KeyValuePair
    {
        public object Key;
        public string Value;
    }

    public class GetFieldOptionsListEventData
    {
        public List<KeyValuePair> List { get; set; }
        public string FieldName { get; set; }
    }

    public interface IDataLayoutExtender
	{
		void onGetQueryable(GetFieldQueryableEventData qd);
        void onGetOptionsList(GetFieldOptionsListEventData qd);
    }


	public class DataLayoutBindingSource<T> : BindingSource, IDataLayoutExtender, IDisposable
	{
		private manager.ILogger _logger =  manager.SLogManager.getInstance().getClassLogger(typeof(DataLayoutBindingSource<T>));

		private DataLayoutControl _cnt;
		
		private Dictionary<string, IList<CustomAttribute>> _attributesCache = new Dictionary<string, IList<CustomAttribute>>();
		private object _oldCurrent = null;
		private bool _layoutIsValid = true;
		private bool _resetLayoutRequest = false;
		
		private IStructureWatcher _structureWatcher = null;
		
		public EventHandler<GetFieldQueryableEventData> GetFieldQueryable;
        public EventHandler<GetFieldOptionsListEventData> GetFieldOptionsList;

        
		public DataLayoutBindingSource() : base()
        {
			start();
        }
		public DataLayoutBindingSource(IContainer c) : base(c)
		{
			start();
		}
		public DataLayoutBindingSource(object o, string s) : base(o, s)
		{
			start();
		}

		private void start()
		{
			CurrentChanged += handleCurrentChanged;
		}

		private void resetDataLayout() {
			// reset layout if
			// there is one active
			if (_cnt != null && DataSource != null && _layoutIsValid)
			{
				if (_logger != null && CurrencyManager.Position >= 0)
					_logger.Debug("Reset layout");
				
				_cnt.DataSource = null;
				_cnt.DataBindings.Clear();
				_cnt.Clear();
				_resetLayoutRequest = false;
				_layoutIsValid = false;
				// now set new source
				_cnt.DataSource = this;
            }			
		}

        public void addNewRecord(object rec)
        {
            AddNew();
            base.Current.CopyFrom(rec);
           
        }

		protected override void OnListChanged(ListChangedEventArgs e)
		{
			if (_logger != null && CurrencyManager.Position >= 0)
				_logger.Debug("LC-Current: " + (base.Current != null ? base.Current.GetPropValueByPathUsingReflection("id") : "null"));

			/* eventual possible use
					
			switch (e.ListChangedType)
			{
				case ListChangedType.PropertyDescriptorChanged:
					{
						//we have to refresh attributes cache
						if (DataSource != null && e.PropertyDescriptor == null)
						{
						}

						break;
					}
			}
			*/

			//orig call
			try
			{
				base.OnListChanged(e);
			}
			catch (Exception ex)
			{
				// we can have problems to bind at form cause it can not match new data
				// so stop exception here, cause we are moving to new record
				if (_logger != null && CurrencyManager.Position >= 0)
				{
					_logger.Debug("LC-EXCEPT-Current: (" + ex.Message + ") " + (base.Current != null ? base.Current.GetPropValueByPathUsingReflection("id") : "null"));
				}
			}
			if (_logger != null && CurrencyManager.Position >= 0)
				_logger.Debug("LC-OUT-Current: " + (base.Current != null ? base.Current.GetPropValueByPathUsingReflection("id") : "null"));


		}

		private void handleCurrentChanged(object sender, object args)
		{	
			_logger.Debug("CC-Current ["+sender+"] : " + (base.Current != null ? base.Current.GetPropValueByPathUsingReflection("id") : "null"));


			if (_oldCurrent != base.Current) {
				//de-serialize if necessary
				_logger.Debug("CC-Current Deserialize");

				if(_structureWatcher != null) {
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

			_logger.Debug("CC-OUT-Current: " + (base.Current != null ? base.Current.GetPropValueByPathUsingReflection("id") : "null"));
		}

		public new object DataSource {
			get {
				return base.DataSource; 
			}

			set {
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
				else {
					t = tmpT;
				}
				if(t.IsInstanceOfType(typeof(SerializedEntityBase))) {
					Type generic = typeof(StructureWatcher<>);
					Type[] typeArgs = { t };
					Type tg = generic.MakeGenericType(typeArgs);
					_structureWatcher = (IStructureWatcher)Activator.CreateInstance(tg);
				}
				else {
					_structureWatcher = null;
				}
				// make generic Structure watch basing on type of DataSource element
								
				base.DataSource = value;
				//load fields eventually
				if(!_layoutIsValid) {
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
				if (_logger != null)
					_logger.Debug("Set-DS : New");
				else
				{
					_logger = xwcs.core.manager.SLogManager.getInstance().getClassLogger(GetType());
					_logger.Debug("Set-DS : New");
				}

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
				_layoutIsValid = false;
				//connect
				_cnt.DataSource = this;
				
            }
		}

		private void FieldRetrievedHandler(object sender, FieldRetrievedEventArgs e)
		{
			_logger.Debug("Retrieving for field:" + e.FieldName);
			if (_attributesCache.ContainsKey(e.FieldName))
			{
				foreach (CustomAttribute a in _attributesCache[e.FieldName])
				{
					a.applyRetrievedAttribute(this, e);
				}
			}
			onFieldRetrieved(e);
			
			// at the end say that layout is valid
			// TODO: verify what happen if there is a change in the middle, this is called for each field separately
			_layoutIsValid = true;
		}

		private void FieldRetrievingHandler(object sender, FieldRetrievingEventArgs e)
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();
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

			sw.Stop();
			_logger.Debug(String.Format("Elapsed={0}", sw.Elapsed));

			
			onFieldRetrieving(e);
			// fixed things
			e.DataSourceUpdateMode = DataSourceUpdateMode.OnPropertyChanged;
			e.Handled = true;
		}

		public void onGetQueryable(GetFieldQueryableEventData qd)
		{
			if (GetFieldQueryable != null)
			{
				GetFieldQueryable(this, qd);
			}
		}

        public void onGetOptionsList(GetFieldOptionsListEventData qd)
        {
            if (GetFieldOptionsList != null)
            {
                GetFieldOptionsList(this, qd);
            }
        }

        protected virtual void onFieldRetrieving(FieldRetrievingEventArgs e) { }
		protected virtual void onFieldRetrieved(FieldRetrievedEventArgs e) { }


		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls
		protected override void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					//only if disposing is caled from Dispose patern	
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				//disconnect events in any case
				if (_cnt != null)
				{
					_cnt.FieldRetrieved -= FieldRetrievedHandler;
					_cnt.FieldRetrieving -= FieldRetrievingHandler;
				}

				disposedValue = true;
				//call inherited
				base.Dispose(disposing);
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		~DataLayoutBindingSource()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(false);
		}

		// This code added to correctly implement the disposable pattern.
		/* INHERITED SO NOT USE IT HERE
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		*/
		#endregion
	}
}
