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
    // sender is container of changed property
    /*
     * We have here 2 differen cases for handle
     * 1: nested objects 
     * 2: events generated from objects agregated in child collection
     * 
     * 1. is simple we Add NestedHandler to ModelPropertyChange event when child object is created or changed
     *    to other one
     * 2. is more complicated cause we have container in the middle, so we have handle eventual attach/detach from 
     *    ModelPropertyChanged event
     */
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

            /// <summary>
            ///  Compare 2 entries using its container reference
            /// </summary>
            /// <param name="other"></param>
            /// <returns>true if two entries have the same container</returns>
            public bool Cmp(PropertyChangedChainEntry other)
            {
                return ReferenceEquals(Container, other.Container);
            }
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

        public bool AddInChain(string Name, PropertyChangedChainEntry Prop)
        {
            // we need do protection against event infinite loop 
            // caused by cycles in model graph
            // we have check if there not exists the same container
            // in chain already
            foreach(PropertyChangedChainEntry e in PropertyChain)
            {
                if (e.Cmp(Prop)) return false; // we skip add so we found cycle
            }

            // new entry so just use it
            PropertyChain.Insert(0, Prop);
            PropertyNamesChian.Insert(0, Name);

            return true;
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
        DBContextBase GetCtx();
        void SetCtx(DBContextBase c);
    }

    public class EntityList<T> : BindingList<T>, INotifyModelPropertyChanged, IModelEntity where T : EntityBase
    {
        private int _indexOfLastChangedItem;

        private bool _changed = false;
        public bool IsChanged()
        {
            return _changed;
        }

        private int _currentDataVersion;
        public int GetDataVersion()
        {
            return _currentDataVersion;
        }
        public void SetDataVersion(int value)
        {
            _currentDataVersion = value;
        }

        private DBContextBase _ctx;
        public DBContextBase GetCtx()
        {
            return _ctx;
        }
        public void SetCtx(DBContextBase c)
        {
            _ctx = c;
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
            _indexOfLastChangedItem = -1;
            this.ListChanged += internal_data_ListChanged;
        }
        public EntityList(IList<T> list) : base(list) {
            _indexOfLastChangedItem = -1;
            this.ListChanged += internal_data_ListChanged;
        }

        /*
         * Overrides
         */
        protected override void InsertItem(int index, T item)
        {
            (item as INotifyModelPropertyChanged).ModelPropertyChanged += OnNestedPropertyChanged;
            base.InsertItem(index, item);

            // notify whole object change
            ModelPropertyChangedEventArgs earg = new ModelPropertyChangedEventArgs(
                            "*",
                            new ModelPropertyChangedEventArgs.PropertyChangedChainEntry()
                            {
                                PropertyName = "*",
                                Container = this[index],
                                Collection = this
                            }
            );
            earg.AddInChain(
                index.ToString(),
                new ModelPropertyChangedEventArgs.PropertyChangedChainEntry()
                {
                    PropertyName = index.ToString(),
                    Container = this,
                    Position = index, // in case of delete it will be out of bounds
                    Collection = this,
                    ListChangedType = ListChangedType.ItemAdded
                }
            );
            _wes_ModelPropertyChanged?.Raise(
                this,
                earg
            );
        }
        protected override void RemoveItem(int index)
        {
            (this[index] as INotifyModelPropertyChanged).ModelPropertyChanged -= OnNestedPropertyChanged;
            base.RemoveItem(index);

            // collection changed, due to one element removed
            _wes_ModelPropertyChanged?.Raise(
                this,
                new ModelPropertyChangedEventArgs(
                    "*",
                    new ModelPropertyChangedEventArgs.PropertyChangedChainEntry()
                    {
                        PropertyName = "*",
                        Container = this,
                        Collection = this
                    }
            ));
        }
        protected override void SetItem(int index, T item)
        {
            // detach
            (this[index] as INotifyModelPropertyChanged).ModelPropertyChanged -= OnNestedPropertyChanged;
            // attach
            (item as INotifyModelPropertyChanged).ModelPropertyChanged += OnNestedPropertyChanged;

            base.SetItem(index, item);

            // notify whole object change
            ModelPropertyChangedEventArgs earg = new ModelPropertyChangedEventArgs(
                "*",
                new ModelPropertyChangedEventArgs.PropertyChangedChainEntry()
                {
                    PropertyName = "*",
                    Container = this[index],
                    Collection = this
                }
                
            );
            earg.AddInChain(
                index.ToString(),
                new ModelPropertyChangedEventArgs.PropertyChangedChainEntry()
                {
                    PropertyName = index.ToString(),
                    Container = this,
                    Position = index, // in case of delete it will be out of bounds
                    Collection = this,
                    ListChangedType = ListChangedType.ItemAdded
                }
            );
            _wes_ModelPropertyChanged?.Raise(
                this,
                earg
            );
        }
        protected override void ClearItems()
        {
            foreach(EntityBase e in this)
            {
                (e as INotifyModelPropertyChanged).ModelPropertyChanged -= OnNestedPropertyChanged;
            }
            base.ClearItems();
            // whole collection changed
            _wes_ModelPropertyChanged?.Raise(
                this,
                new ModelPropertyChangedEventArgs(
                    "*",
                    new ModelPropertyChangedEventArgs.PropertyChangedChainEntry()
                    {
                        PropertyName = "*",
                        Container = this,
                        Collection = this
                    }
            ));
        }

		

		private void internal_data_ListChanged(object sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.ItemChanged)
            {
                // we just take index of changed element, cause 
                // just after there will come model property change
                // and it will use it
                _indexOfLastChangedItem = e.NewIndex;
            }
        }

		private void OnNestedPropertyChanged(object sender, ModelPropertyChangedEventArgs e)
		{
			INotifyModelPropertyChanged s = sender as INotifyModelPropertyChanged;

			// if not recognized return
			if (s == null) return;

			// add root type name to nested property changed event
            // but only if we are not in cycle
			if(e.AddInChain(_indexOfLastChangedItem.ToString(),
                new ModelPropertyChangedEventArgs.PropertyChangedChainEntry()
                {
                    PropertyName = _indexOfLastChangedItem.ToString(),
                    Position = _indexOfLastChangedItem,
                    Container = this,
                    Collection = this
                }
            ))
            {
                // handle changed
                _changed = true; // false positive it will do true even if object was empty

                // notify
                _wes_ModelPropertyChanged?.Raise(this, e);
            }			
		}

        public object GetModelPropertyValueByName(string PropName)
        {
            int l = PropName.IndexOf(".");
            string name = PropName;
            string suffix = "";
            if (l > 0)
            {
                name = PropName.Substring(0, l);
                suffix = PropName.Substring(l + 1);
            }

            int idx;
            if(int.TryParse(name, out idx))
            {
                object obj = this[idx];
                if (suffix != "" && obj is IModelEntity)
                {
                    return (obj as IModelEntity).GetModelPropertyValueByName(suffix);
                }

                //done
                return obj;
            }

            //not done			
            return null;
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

        private DBContextBase _ctx = null;
        public DBContextBase GetCtx()
        {
            return _ctx;
        }
        public void SetCtx(DBContextBase c)
        {
            _ctx = c;
        } 

        /// <summary>
        ///  this is convetion, we dont have name settings generated in model
        ///  and properties have type name as proper name
        /// </summary>
        /// <returns></returns>
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

        public virtual int GetLockId()
        {
            object idFiled = GetModelPropertyValueByName("id");
            return ReferenceEquals(idFiled, null) ? -1 : (int)idFiled;
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
            MethodBase info = new StackFrame(3).GetMethod();
            _logger.Debug(string.Format("{0}.{1} -> {2}", info.DeclaringType?.Name, info.Name, propertyName));
#endif

            storage = value;
            OnPropertyChanged(propertyName, storage);
        }

        
        protected void SetNavigProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null) where T : INotifyModelPropertyChanged
        {
            // skip if not changed
            if (ReferenceEquals(storage, value)) return;

#if DEBUG_TRACE_LOG_ON
            MethodBase info = new StackFrame(3).GetMethod();
            _logger.Debug(string.Format("{0}.{1} -> {2}", info.DeclaringType?.Name, info.Name, propertyName));
#endif
            // handle real change
            if (!ReferenceEquals(null, storage))
            {
                storage.ModelPropertyChanged -= OnNestedPropertyChanged;
            }
            storage = value;
            if (!ReferenceEquals(null, storage))
            {
                storage.ModelPropertyChanged += OnNestedPropertyChanged;
            }
            // notify that nested was changed
            OnPropertyChanged(propertyName, storage);
        }


        protected void OnPropertyChanged(string propertyName = null, object value = null)
        {

            _changed = true; // false positive it will do true even if object was empty

#if DEBUG_TRACE_LOG_ON
            _logger.Debug(string.Format("Property changed {0} from  {1}", propertyName, GetType().Name));
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

        private void OnNestedPropertyChanged(object sender, ModelPropertyChangedEventArgs e)
        {
            INotifyModelPropertyChanged s = sender as INotifyModelPropertyChanged;

            // if not recognized return
            if (s == null) return;
            // add root type name to nested property changed event
            if (e.AddInChain(s.GetFieldName(),
                            new ModelPropertyChangedEventArgs.PropertyChangedChainEntry()
                            {
                                PropertyName = s.GetFieldName(),
                                Container = this
                            }
            ))
            {
                // handle changed
                _changed = true; // false positive it will do true even if object was empty
                _wes_ModelPropertyChanged?.Raise(this, e);
            }
        }

        public object GetModelPropertyValueByName(string PropName)
        {
            int l = PropName.IndexOf(".");
            string name = PropName;
            string suffix = "";
            if (l > 0)
            {
                name = PropName.Substring(0, l);
                suffix = PropName.Substring(l + 1);
            }

            object obj = this.GetType()
                        .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(field => field.Name == '_' + name)
                        .Select(field => field.GetValue(this))
                        .FirstOrDefault();

            //not done			
            if (suffix != "" && obj is IModelEntity)
            {
                return (obj as IModelEntity).GetModelPropertyValueByName(suffix);
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
