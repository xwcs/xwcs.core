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
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics;
    using System.Linq.Expressions;
#if DEBUG_TRACE_LOG_ON
    using System.Diagnostics;
#endif
    using System.Reflection;
    using System.Text;

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
    public class ModelPropertyChangedEventArgs : EventArgs
    {
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
            foreach (PropertyChangedChainEntry e in PropertyChain)
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

    public interface IProxyable
    {
        int id { get; }
    }


    public class EntityList<T> : BindingList<T>, INotifyModelPropertyChanged, IModelEntity where T : EntityBase
    {
        private int _indexOfLastChangedItem;

        /// <summary>
        /// this property say if object was modified, it
        /// </summary>
        private bool _changed = false;
        public bool IsChanged()
        {
            return _changed;
        }
        protected void SetChaged()
        {
            _changed = true;
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

        private WeakReference _ctx = null;
        public DBContextBase GetCtx()
        {
            return _ctx != null && _ctx.IsAlive ? (DBContextBase)_ctx.Target : null;
        }
        public void SetCtx(DBContextBase c)
        {
            // context must be assigned max 1 time
            if (!ReferenceEquals(null, _ctx))
            {
                throw new ApplicationException("Db context can be assigned just one time!");
            }
            _ctx = new WeakReference(c);
        }

        public string GetFieldName()
        {
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

        public EntityList()
        {
            _indexOfLastChangedItem = -1;
            this.ListChanged += internal_data_ListChanged;
        }
        public EntityList(IList<T> list) : base(list)
        {
            _indexOfLastChangedItem = -1;
            this.ListChanged += internal_data_ListChanged;
        }

        /*
         * Overrides
         */
        protected override void InsertItem(int index, T item)
        {
            SetChaged();

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
            SetChaged();

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
            SetChaged();

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
            SetChaged();

            foreach (EntityBase e in this)
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
            if (e.AddInChain(_indexOfLastChangedItem.ToString(),
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
                SetChaged();

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
            if (int.TryParse(name, out idx))
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
    public abstract class EntityBase : BindableObjectBase, INotifyModelPropertyChanged, IModelEntity
    {
        private WeakReference _ctx = null;
        public DBContextBase GetCtx()
        {
            return !ReferenceEquals(null, _ctx) && _ctx.IsAlive ? (DBContextBase)_ctx.Target : null;
        }
        public virtual void SetCtx(DBContextBase c)
        {
            if (ReferenceEquals(null, c)) {
                _ctx = null;
            } else {
                // context must be assigned max 1 time
                if (!ReferenceEquals(null, _ctx) && !ReferenceEquals(c, GetCtx()))
                {
                    throw new ApplicationException("Db context can be assigned just one time!");
                }
                // maby done
                if (ReferenceEquals(c, GetCtx())) return;

                _ctx = new WeakReference(c);
            }
            // reset original values
            _originalValues = null;
        }

        /// <summary>
        /// Comparision o original propertyvalues
        /// </summary>
        private System.Data.Entity.Infrastructure.DbPropertyValues _originalValues = null;
        public bool GetIsReallyChanged(bool ForceDetectChanges = false)
        {
            // cache original values
            if(ReferenceEquals(_originalValues, null))
            {
                DBContextBase ctx = null;
                bool oldState = true; // if something goes wrong it will force TRUE, it can slow down program, but cant break anything
                try{
                    ctx = GetCtx();
                    if (ctx == null) throw new ApplicationException("GetIsReallyChanged need db context!");

                    // defaultly off
                    oldState = ctx.Configuration.AutoDetectChangesEnabled;
                    ctx.Configuration.AutoDetectChangesEnabled = ForceDetectChanges;

                    System.Data.Entity.Infrastructure.DbEntityEntry<EntityBase> _Entry = ctx.Entry(this as EntityBase);
                    if (ReferenceEquals(_Entry, null) || ReferenceEquals(_Entry.OriginalValues, null))
                    {
                        throw new ApplicationException("GetIsReallyChanged can't identify entity in db contet!");
                    }
                    _originalValues = _Entry.OriginalValues;
                }finally{
                    // be sure autodetect is on
                    if(!ReferenceEquals(ctx, null))
                    {
                        ctx.Configuration.AutoDetectChangesEnabled = oldState;
                    }
                }

                if (ReferenceEquals(_originalValues, null))
                    throw new ApplicationException("GetIsReallyChanged can't work without original values!");
            }             
               
                    
            /* just use original values and manually test */
            foreach (string en in _originalValues.PropertyNames)
            {
                if (!Equals(_originalValues[en], GetPropertyValue(en)))
                {
                    return true;
                }
            }
            // not changed
            return false;
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

        protected object GetOriginalPropertyValue(string propertyName)
        {
            if (ReferenceEquals(_originalValues ,null)) return GetPropertyValue(propertyName);
            return _originalValues[propertyName];
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


        protected override void OnPropertyChanged(string propertyName = null, object value = null)
        {

            SetChaged();

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
                SetChaged();
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


    [TypeDescriptionProvider(typeof(HyperTypeDescriptionProvider))]
    public abstract class BindableObjectBase : INotifyPropertyChanged, IValidableEntity
    {
        protected static manager.ILogger _logger = manager.SLogManager.getInstance().getClassLogger(typeof(EntityBase));
        protected TypeCacheData _tcd;

        /// <summary>
        /// this property say if object was modified
        /// </summary>
        private bool _changed = false;
        public bool IsChanged()
        {
            return _changed;
        }
        protected void SetChaged()
        {
            _changed = true;
        }
        
        public BindableObjectBase()
        {
            _tcd = TypeCache.GetTypeCacheData(GetType());
        }

        protected WeakEventSource<PropertyChangedEventArgs> _wes_PropertyChanged = null;
        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                if (_wes_PropertyChanged == null)
                {
                    _wes_PropertyChanged = new WeakEventSource<PropertyChangedEventArgs>();
                }
                _wes_PropertyChanged.Subscribe(value);
            }
            remove
            {
                _wes_PropertyChanged?.Unsubscribe(value);
            }
        }

        /// <summary>
        /// Direct value field
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="storage"></param>
        /// <param name="value"></param>
        /// <param name="propertyName"></param>
        protected virtual void SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
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


        protected virtual void OnPropertyChanged(string propertyName = null, object value = null)
        {

            SetChaged();

#if DEBUG_TRACE_LOG_ON
            _logger.Debug(string.Format("Property changed {0} from  {1}", propertyName, GetType().Name));
#endif
            _wes_PropertyChanged?.Raise(this, new PropertyChangedEventArgs(propertyName));
        }


        #region reflection

        /*
         * Lets say we need access all property getters with array[name] notation
         */
       
        protected static void InitReflectionChache(Type who)
        {
            TypeCache.GetTypeCacheData(who);
        }

        //we do delegates
        public object GetPropertyValue(string pName)
        {
            
            PropertyDescriptor pd;
            if(_tcd.Pds.TryGetValue(pName, out pd))
            {
                return pd.GetValue(this);
            }
            return null;
        }

        public void SetPropertyValue(string pName, object value)
        {
            PropertyDescriptor pd;
            if (_tcd.Pds.TryGetValue(pName, out pd))
            {
                pd.SetValue(this, value);
            }
        }

        
        #endregion


        #region IValidableEntity
        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            foreach (var pi in _tcd.GetPropertiesWithAttributeType(typeof(binding.attributes.CheckValidAttribute)))
            {
				Problem pr = ValidateProperty(pi.Name);
                if (pr.Kind != ProblemKind.None)
                {
                    yield return pr;
                }
            }
        }
        public virtual Problem ValidateProperty(string pName, object newValue)
        {
            // convert new value to correct type
            PropertyInfo pi = _tcd.Properties[pName];
            // handle null also
            if (newValue != null && (newValue is string || newValue.GetType() != pi.PropertyType)) {
                newValue = TConvert.ChangeType(newValue, pi.PropertyType);
            }
            return ValidatePropertyInternal(pName, newValue);
        }
        protected virtual Problem ValidatePropertyInternal(string pName, object newValue)
        {
            return Problem.Success;
        }
        public virtual Problem ValidateProperty(string pName)
        {
            return ValidateProperty(pName, GetPropertyValue(pName));
        }

        public bool IsValid()
        {			
			foreach (var vr in Validate(new ValidationContext(this)).Cast<Problem>())
            {
                if (vr.Kind != ProblemKind.None) return false;
            }

            return true;
        }

        public string ErrorMessage(string separator = "\n")
        {
            List<string> erlist = Validate(new ValidationContext(this)).Select(c => c.ToString()).ToList();
            return string.Join(separator, erlist);
        }
        #endregion
    }


    

    public class EntityProxy<TEntity> : EntityBase, IProxyable where TEntity : class, IProxyable
    {

        protected System.Data.Entity.DbSet<TEntity> _srcList = null;
        private TEntity _destination = null;
        protected bool _attachedToDb = false;
        protected string _RootEntityPropertyName = "";

        public TEntity Entity
        {
            get
            {
                return _destination;
            }
        }

        public override void SetCtx(DBContextBase ctx)
        {
            base.SetCtx(ctx);
            _srcList = ctx.GetPropertyByName(_RootEntityPropertyName) as System.Data.Entity.DbSet<TEntity>;
            _attachedToDb = true;
        }

        public virtual int id { get; set; }

        /// <summary>
        /// This method can be use for locking
        /// it is called in setter so it can throw and set will be refused
        /// </summary>
        /// <returns></returns>
        protected virtual void AttachEntity()
        {
            // load entity from db
            _destination = _srcList.Where(e => e.id == this.id).FirstOrDefault();
        }

        /// <summary>
        /// this method can be use for unlock
        /// must be called from outside
        /// </summary>
        public virtual void DetachEntity()
        {
            _destination = null;
        }




        /// <summary>
        /// Direct value field
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="storage"></param>
        /// <param name="value"></param>
        /// <param name="propertyName"></param>
        protected T GetProperty<T>(ref T storage, [CallerMemberName] string propertyName = null)
        {

#if DEBUG_TRACE_LOG_ON
            _logger.Debug(string.Format("{0} -> {1}",  xwcs.core.manager.SLogManager.DumpCallStack(1, 2), propertyName));
#endif
            if (_attachedToDb && !ReferenceEquals(_destination, null))
            {
                return (T)(_destination as EntityBase).GetPropertyValue(propertyName);
            }
            else
            {
                return storage;
            }
        }

        /// <summary>
        /// Direct value field
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="storage"></param>
        /// <param name="value"></param>
        /// <param name="propertyName"></param>
        protected override void SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            // skip if not changed
            if (Equals(storage, value)) return;

#if DEBUG_TRACE_LOG_ON
            _logger.Debug(string.Format("{0} -> {1}", xwcs.core.manager.SLogManager.DumpCallStack(1, 2), propertyName));
#endif
            // now here i need load real object
            if (_attachedToDb)
            {
                // if not attached attach it
                if (ReferenceEquals(_destination, null))
                {
                    AttachEntity();
                    if (ReferenceEquals(_destination, null))
                    {
                        // missing record after we load view
                        throw new KeyNotFoundException("Record deleted");
                    }
                }
                (_destination as EntityBase).SetPropertyValue(propertyName, value);
            }
            else
            {
                storage = value;
            }
            OnPropertyChanged(propertyName, storage);
        }


        protected override Problem ValidatePropertyInternal(string pName, object newValue)
        {
            if (_attachedToDb)
            {
                // if not attached attach it
                if (ReferenceEquals(_destination, null))
                {
                    AttachEntity();
                    if (ReferenceEquals(_destination, null))
                    {
                        // missing record after we load view
                        throw new KeyNotFoundException("Record deleted");
                    }
                }
                return (_destination as EntityBase).ValidateProperty(pName, newValue);
            }

            return Problem.Success;
        }
    }

    internal static class TConvert
    {
        public static T ChangeType<T>(object value)
        {
            return (T)ChangeType(value, typeof(T));
        }
        public static object ChangeType(object value, Type t)
        {
            try
            {
                TypeConverter tc = TypeDescriptor.GetConverter(t);
                return tc.ConvertFrom(value);
            }catch(Exception)
            {
                // here we should log maby
                throw;
            }            
        }
        /*
        public static void RegisterTypeConverter<T, TC>() where TC : TypeConverter
        {
            TypeDescriptor.AddAttributes(typeof(T), new TypeConverterAttribute(typeof(TC)));
        }
        */
    }
}