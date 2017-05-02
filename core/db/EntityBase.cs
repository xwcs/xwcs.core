using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;

namespace xwcs.core.db
{
    using evt;
    using model;
    using model.attributes;
    using System.Collections.Generic;
#if DEBUG_TRACE_LOG_ON
    using System.Diagnostics;
#endif
    using System.Reflection;


    public enum ModelPropertyChangedEventKind
    {
        Default = 0,
        SetValue = 1,
        Reset = 2
    }

    // structured changes in model will propagate this event
    public class ModelPropertyChangedEventArgs : EventArgs {
        public class PropertyChangedChainEntry
        {
            public object Container = null;
            public string PropertyName = "";
            public PropertyDescriptor PropertyDescriptor = null; // eventually this
            public int Position = -1; // used in nested Collections
            public object Collection = null;
            public object Value = null;
            public ListChangedType ListChangedType = ListChangedType.ItemChanged; // default is this, in case of list change this will change
        }
        
        public List<PropertyChangedChainEntry> PropertyChain = new List<PropertyChangedChainEntry>();
        public List<string> PropertyNamesChian = new List<string>();
        private ModelPropertyChangedEventKind _changeKind = ModelPropertyChangedEventKind.Default;

        public ModelPropertyChangedEventArgs(string Name, PropertyChangedChainEntry Prop)
        {
            AddInChain(Name, Prop);
        }

        public ModelPropertyChangedEventArgs(string Name, PropertyChangedChainEntry Prop, ModelPropertyChangedEventKind kind) : this(Name, Prop)
        {
            _changeKind = kind;
        }

        public ModelPropertyChangedEventKind ChangeKind
        {
            get
            {
                return _changeKind;
            }
        }

        public void AddInChain(string Name, PropertyChangedChainEntry Prop)
        {
            PropertyChain.Insert(0, Prop);
            PropertyNamesChian.Insert(0, Name);
        }

        public override string ToString()
        {
            return string.Join(".", PropertyNamesChian);
        }

        public bool HasWildCharInName()
        {
            return ToString().Contains("*");
        }

        public string ToRegExp()
        {
            return (PropertyNamesChian.Count == 1 && PropertyNamesChian[0] == "*") ?
                        @"^(?:(?:[^\.]*?)\.?)+$" :
                        string.Format(@"^{0}$", string.Join(@"\.", PropertyNamesChian.Select(n => n.Replace("*", @"[^\.]*?"))));
        }

        public object Value
        {
            get
            {
                return PropertyChain[PropertyChain.Count - 1].Value;
            }
        }
    }
    public interface INotifyModelPropertyChanged
    {
        event EventHandler<ModelPropertyChangedEventArgs> ModelPropertyChanged;
        bool IsChanged();
        string GetFieldName();
    }


    public class CurrentObjectChangedEventArgs : EventArgs
    {
        public object Old = null;
        public object Current = null;
    }

    public interface INotifyCurrentObjectChanged
    {
        event EventHandler<CurrentObjectChangedEventArgs> CurrentObjectChanged;
    }


    public class PropertyDeserialized : EventArgs
    {
        public PropertyDeserialized(object property, string propertyName)
        {
            SourceProperty = property;
            SourcePropertyName = propertyName;
        }
        public object SourceProperty { get; private set; }
        public string SourcePropertyName { get; private set; }
    }


    public interface IModelEntity
    {
        object GetModelPropertyValueByName(string PropName);
    }

    public class EntityList<T> : BindingList<T>, INotifyModelPropertyChanged where T : EntityBase
    {
        private bool _changed = false;
        public bool IsChanged()
        {
            return _changed;
        }

        public string GetFieldName() {
            return typeof(T).Name;
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

        public EntityList() : base()  {
            this.ListChanged += internal_data_ListChanged;
        }
        public EntityList(IList<T> list) : base(list) {
            this.ListChanged += internal_data_ListChanged;
        }



        private void internal_data_ListChanged(object sender, ListChangedEventArgs e)
        {
			// when entity is added in list we need connect ourself to its model property chaged event
			if (e.ListChangedType == ListChangedType.ItemAdded) {
				//this[e.NewIndex].ModelPropertyChanged += OnNestedPropertyChanged;
			}


            if (e.ListChangedType == ListChangedType.ItemChanged || 
                e.ListChangedType == ListChangedType.ItemAdded ||
                e.ListChangedType == ListChangedType.ItemDeleted || 
                e.ListChangedType == ListChangedType.ItemMoved)
            {

                // handle changed
                _changed = true;

                if (e.PropertyDescriptor != null)
                {
                    //froward event 
                    _wes_ModelPropertyChanged?.Raise(
                            this,
                            new ModelPropertyChangedEventArgs(
                                e.PropertyDescriptor.Name,
                                new ModelPropertyChangedEventArgs.PropertyChangedChainEntry()
                                {
                                    Container = this[e.NewIndex],
                                    PropertyDescriptor = e.PropertyDescriptor,
                                    PropertyName = e.PropertyDescriptor.Name,
                                    Position = e.NewIndex,
                                    Collection = this,
                                    ListChangedType = e.ListChangedType 
                                }
                            )
                   );
                }
                else
                {
                    _wes_ModelPropertyChanged?.Raise(
                            this,
                            new ModelPropertyChangedEventArgs(
                                "*",
                                new ModelPropertyChangedEventArgs.PropertyChangedChainEntry()
                                {
                                    Container = e.NewIndex < Count ? this[e.NewIndex] : null,
                                    Position = e.NewIndex, // in case of delete it will be out of bounds
                                    Collection = this,
                                    ListChangedType = e.ListChangedType
                                }
                            )
                       );
                }
            }
        }

		private void OnNestedPropertyChanged(object sender, ModelPropertyChangedEventArgs e)
		{
			INotifyModelPropertyChanged s = sender as INotifyModelPropertyChanged;

			// if not recoginzed return
			if (s == null) return;
			// add root type name to nested property changed event
			e.AddInChain(s.GetFieldName(), new ModelPropertyChangedEventArgs.PropertyChangedChainEntry()
			{
				PropertyName = s.GetFieldName(),
				Container = this
			});

			// handle changed
			_changed = true; // false positive it will do true even if object was empty

			// notify
			_wes_ModelPropertyChanged?.Raise(this, e);
		}
	}


	[TypeDescriptionProvider(typeof(HyperTypeDescriptionProvider))]
	public abstract class EntityBase : INotifyPropertyChanged, INotifyModelPropertyChanged, IModelEntity
    {
        protected static manager.ILogger _logger = manager.SLogManager.getInstance().getClassLogger(typeof(EntityBase));

        private bool _changed = false;
        public bool IsChanged()
        {
            return _changed;
        }

        public string GetFieldName()
        {
            return GetType().Name;
        }

        public EntityBase()
        {
            //call eventual init in partial
            MethodInfo mi = GetType().GetMethod("InitPartial");
            if (mi != null)
            {
                mi.Invoke(this, null);
            }
        }

        private WeakEventSource<PropertyChangedEventArgs> _wes_PropertyChanged = null;
        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                if (_wes_PropertyChanged == null)
                {
                    _wes_PropertyChanged = new WeakEventSource<PropertyChangedEventArgs>();
                }
                _wes_PropertyChanged.SubscribePropertyChanged(value);
            }
            remove
            {
                _wes_PropertyChanged?.UnsubscribePropertyChanged(value);
            }
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

        /// <summary>
        /// Direct value field
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="storage"></param>
        /// <param name="value"></param>
        /// <param name="propertyName"></param>
        protected void SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            // skip if not changed
            if (Equals(storage, value)) return;

#if DEBUG_TRACE_LOG_ON
            //MethodBase info = new StackFrame(3).GetMethod();
            //_logger.Debug(string.Format("{0}.{1} -> {2}", info.DeclaringType?.Name, info.Name, propertyName));
#endif

            storage = value;
            OnPropertyChanged(propertyName, storage);
        }

        private void OnNestedPropertyChanged(object sender, ModelPropertyChangedEventArgs e)
        {
            INotifyModelPropertyChanged s = sender as INotifyModelPropertyChanged;

            // if not recoginzed return
            if (s == null) return; 
            // add root type name to nested property changed event
            e.AddInChain(s.GetFieldName(), new ModelPropertyChangedEventArgs.PropertyChangedChainEntry()
            {
                PropertyName = s.GetFieldName(),
                Container = this
            });

            // handle changed
            _changed = true; // false positive it will do true even if object was empty

            // notify
            _wes_ModelPropertyChanged?.Raise(this, e);

			// we need forward also Property chaged
			OnPropertyChanged(s.GetFieldName(), sender);
        }

        protected void SetNavigProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null) where T : INotifyModelPropertyChanged
        {
            // skip if not changed
            if (ReferenceEquals(storage, value)) return;
#if DEBUG_TRACE_LOG_ON
            //MethodBase info = new StackFrame(3).GetMethod();
            //_logger.Debug(string.Format("{0}.{1} -> {2}", info.DeclaringType?.Name, info.Name, propertyName));
#endif
            // handle real change
            if (!ReferenceEquals(null, storage))
            {
                //remove old changed handler
                storage.ModelPropertyChanged -= OnNestedPropertyChanged;
            }

            storage = value;
            // new handler
            storage.ModelPropertyChanged += OnNestedPropertyChanged;
            // notify that nested was changed
            OnPropertyChanged(propertyName, storage);
        }
        

        protected void OnPropertyChanged(string propertyName = null, object value = null)
        {

            // handle changed
            _changed = true; // false positive it will do true even if object was empty

#if DEBUG_TRACE_LOG_ON
            //_logger.Debug(string.Format("Property changed {0} from  {1}", propertyName, GetType().Name));
#endif
            _wes_PropertyChanged?.Raise(this, new PropertyChangedEventArgs(propertyName));

            // model changes
            _wes_ModelPropertyChanged?.Raise(
                this,
                new ModelPropertyChangedEventArgs(
                    propertyName == string.Empty ? "*" : propertyName,
                    new ModelPropertyChangedEventArgs.PropertyChangedChainEntry()
                    {
                        Container = this,
                        PropertyName = propertyName == string.Empty ? "*" : propertyName,
                        Value = value
                    }
                )
            );
        }

        public object GetModelPropertyValueByName(string PropName)
        {
            return GetModelPropertyValueByNameInternal(this, PropName);
        }

        protected object GetModelPropertyValueByNameInternal(object obj, string path)
        {
            if (obj == null) { return null; }

            int l = path.IndexOf(".");
            string name = path;
            string suffix = "";
            if (l > 0)
            {
                name = path.Substring(0, l);
                suffix = path.Substring(l + 1);
            }

            try
            {
                obj = obj.GetType()
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(field => field.Name == '_' + name)
                .Select(field => field.GetValue(obj))
                .Single();
            }
            catch (Exception)
            {
                return null;
            }

            //not done			
            if (suffix != "")
            {
                return GetModelPropertyValueByNameInternal(obj, suffix);
            }

            //done
            return obj;
        }

        protected void BindToNesteds()
        {
            // fields   
            GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(f => f.FieldType.IsSubclassOfRawGeneric(typeof(EntityBase)) || f.FieldType.IsSubclassOfRawGeneric(typeof(EntityList<>)))
            .Select(field => field.GetValue(this))
            .Cast<INotifyModelPropertyChanged>()
            .ToList()
            .ForEach(c =>
            {
                if (!ReferenceEquals(null, c))
                {
                    //remove old changed handler
                    c.ModelPropertyChanged += OnNestedPropertyChanged;
                }
            });
        }
    }

	public abstract class SerializedEntityBase : EntityBase
	{
		
		//public event PropertyDeserializedEventHandler OnPropertyDeserialized;
		private readonly WeakEventSource<PropertyDeserialized> _wes_OnPropertyDeserialized = new WeakEventSource<PropertyDeserialized>();
		public event EventHandler<PropertyDeserialized> OnPropertyDeserialized
		{
			add { _wes_OnPropertyDeserialized.Subscribe(value); }
			remove { _wes_OnPropertyDeserialized.Unsubscribe(value); }
		}


		public abstract void GetMutablePropertiesType(Dictionary<string, Type> dest);

		// return string : we will dump object to string, but we will do it only if source is not NULL
		// we cant reset value using object, this can be reset just setting empty string in dump property
		protected string SerializeAndGet(object source, ref string storage, [CallerMemberName] string propertyName = null)
		{
			if (storage is string && source != null)
			{
				storage = source.TypedSerialize(propertyName, SerializeKind.XmlSerialization);
			}
			return storage;
		}

		// return object : we will de-serialize string into object but only if there is not de-serialized other one
		// cause we use lazy de-serializing, we do this just first time called
		protected object GetOrDeserialize(string source, string sourcePropertyName, ref object storage, [CallerMemberName] string propertyName = null)
		{
			if (storage == null && source != null && source.Length > 0)
			{
				storage = source.TypedDeserialize(sourcePropertyName, SerializeKind.XmlSerialization);
			}
			else
			if (storage == null && (source == null || source.Length == 0))
			{
				storage = null;
			}

			_wes_OnPropertyDeserialized.Raise(this, new PropertyDeserialized(storage, propertyName));

			return storage;
		}

		public abstract void DeserializeFields();
		public abstract void SerializeFields();
	}
}
