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
	public class GetFieldQueryableEventData
	{
		public object Current { get; set; }
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
		public object Current { get; set; }
		public List<KeyValuePair> List { get; set; }
        public string FieldName { get; set; }
    }

    public interface IDataLayoutExtender
	{
		void onGetQueryable(object sender, GetFieldQueryableEventData qd);
        void onGetOptionsList(object sender, GetFieldOptionsListEventData qd);
		object Current { get; }
    }

	public interface IEditorsHost
	{
		void onGetQueryable(object sender, GetFieldQueryableEventData qd);
	}

	public class DataLayoutBindingSource : BindingSource, IDataLayoutExtender, IDisposable
	{
		private static manager.ILogger _logger =  manager.SLogManager.getInstance().getClassLogger(typeof(DataLayoutBindingSource));

		private DataLayoutControl _cnt = null;
		private Type  _dataType;


		private Dictionary<string, IList<CustomAttribute>> _attributesCache = new Dictionary<string, IList<CustomAttribute>>();
		private object _oldCurrent = null;
		private bool _fieldsAreRetrieved = true;
		private bool _resetLayoutRequest = false;
		
		//if we work with serialized entities
		private StructureWatcher _structureWatcher = null;

		//public event EventHandler<GetFieldQueryableEventData> GetFieldQueryable;
		private readonly WeakEventSource<GetFieldQueryableEventData> _wes_GetFieldQueryable = new WeakEventSource<GetFieldQueryableEventData>();
		public event EventHandler<GetFieldQueryableEventData> GetFieldQueryable
		{
			add { _wes_GetFieldQueryable.Subscribe(value); }
			remove { _wes_GetFieldQueryable.Unsubscribe(value); }
		}
		//public event EventHandler<GetFieldOptionsListEventData> GetFieldOptionsList;
		private readonly WeakEventSource<GetFieldOptionsListEventData> _wes_GetFieldOptionsList = new WeakEventSource<GetFieldOptionsListEventData>();
		public event EventHandler<GetFieldOptionsListEventData> GetFieldOptionsList
		{
			add { _wes_GetFieldOptionsList.Subscribe(value); }
			remove { _wes_GetFieldOptionsList.Unsubscribe(value); }
		}



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
							_logger.Error("Missing DataSource for data layout");
							return; // no valid binding arrived so we skip 
						}
					}
				}
				else {
					t = tmpT;
				}

				bool isSerialized = typeof(SerializedEntityBase).IsAssignableFrom(t);

				if (isSerialized) {
					
					if(_structureWatcher != null) {
						if(!_structureWatcher.IsCompatible(t)) {
							_structureWatcher = new StructureWatcher(t);
						}
					}
					else {
						_structureWatcher = new StructureWatcher(t);
					}
				}
				else {
					_structureWatcher = null;
				}
				// make generic Structure watch basing on type of DataSource element
				base.DataSource = value;
				_dataType = t;

				// load fields eventually, layout should be assigned before
				// so we need do eventually also this
				if(!_fieldsAreRetrieved) {
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
			onFieldRetrieved(e);
			
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
			
			onFieldRetrieving(e);
			// fixed things
			e.DataSourceUpdateMode = DataSourceUpdateMode.OnPropertyChanged;
			e.Handled = true;
		}

		public void onGetQueryable(object sender, GetFieldQueryableEventData qd)
		{
			//set current edited object into event
			qd.Current = Current;
			_wes_GetFieldQueryable.Raise(sender, qd);
		}

        public void onGetOptionsList(object sender, GetFieldOptionsListEventData qd)
        {
			//set current edited object into event
			qd.Current = Current;
			_wes_GetFieldOptionsList.Raise(sender, qd);
		}

        protected virtual void onFieldRetrieving(FieldRetrievingEventArgs e) { }
		protected virtual void onFieldRetrieved(FieldRetrievedEventArgs e) { }


		#region IDisposable Support
		protected bool disposedValue = false; // To detect redundant calls
		protected override void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					//only if disposing is called from Dispose pattern

					//disconnect events in any case
					if (_cnt != null)
					{
						_cnt.FieldRetrieved -= FieldRetrievedHandler;
						_cnt.FieldRetrieving -= FieldRetrievingHandler;
						_cnt = null;
					}

					if(DataSource != null) {
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


		/*
        protected override void OnListChanged(ListChangedEventArgs e)
		{

#if DEBUG
			if (CurrencyManager.Position >= 0)
				_logger.Debug("LC-Current: " + (base.Current != null ? base.Current.GetPropValueByPathUsingReflection("id") : "null"));
#endif
			
			//orig call
			try
			{
				base.OnListChanged(e);
			}
			catch (Exception ex)
			{
				// we can have problems to bind at form cause it can not match new data
				// so stop exception here, cause we are moving to new record
				if (CurrencyManager.Position >= 0)
				{
					_logger.Error("LC-EXCEPT-Current: (" + ex.Message + ") " + (base.Current != null ? base.Current.GetPropValueByPathUsingReflection("id") : "null"));
				}
			}
#if DEBUG
			if (CurrencyManager.Position >= 0)
				_logger.Debug("LC-OUT-Current: " + (base.Current != null ? base.Current.GetPropValueByPathUsingReflection("id") : "null"));
#endif

		}
		*/

	}
}
