using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;

namespace xwcs.core.db.fo
{
    using DevExpress.Data.Filtering;
    using model;
    using model.attributes;
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using System.Xml;
    using System.Xml.Schema;
    using System.Runtime.Serialization;
    using System.Reflection;
    using evt;
    using System.Diagnostics;
    using System.Reflection.Emit;

    public class fake_filter_field
    {
        public string name;
        public Type type;
    }

    /*
	 * NOTE:
	 *	Filter object list must be BindingList => so Binding Source can handle change of values
	 */
    [CollectionDataContract(IsReference = true)]
	public class FilterObjectList<T> : BindingList<T> where T : ICriteriaTreeNode {}

	[DataContract(IsReference = true)]
	public class FilterObjectsCollection <T> : ICriteriaTreeNode where T : ICriteriaTreeNode
	{
		#region serialize
		[DataMember(Order = 0)]
		private FilterObjectList<T> _data;
		[DataMember(Order = 1)]
		private string _fieldName;
		[DataMember(Order = 2)]
		private string _fieldFullName;

		#endregion

		#region ICriteriaTreeNode
		public CriteriaOperator GetCondition()
		{
            //return _data.Count > 0 ? new ContainsOperator(GetFullFieldName(), CriteriaOperator.And(_data.Select(o => o.GetCondition()).AsEnumerable())) : null;
            return _data.Count > 0 ? CriteriaOperator.And(_data.Select(o => new ContainsOperator(GetFullFieldName(), o.GetCondition())).AsEnumerable()) : null;
                
        }
		public string GetFullFieldName()
		{
			return _fieldFullName;
		}
		public string GetFieldName()
		{
			return _fieldName;
		}
		public bool HasCriteria()
		{
			return true;
		}
		public void Reset()
		{
			Data = null;
		}
        
        #endregion


        //private need for de-serialize
        private FilterObjectsCollection() : this("", "") {}
		public FilterObjectsCollection(string pn, string fn) : base()
		{
			_data = new FilterObjectList<T>();
			_fieldName = fn;
			_fieldFullName = pn != "" ? pn + "." + fn : fn;
		}

		public ICollection<T> Data {
			get {
				return _data;
			}
			set {
                if(ReferenceEquals(_data, value))
                {
                    return;
                }
				_data.Clear();
				if(value != null)
					value.ToList().ForEach(e => _data.Add(e));
			}
		}		
	} 


	[TypeDescriptionProvider(typeof(HyperTypeDescriptionProvider))]
	[DataContract(IsReference = true)]
	public abstract class FilterObjectbase : INotifyPropertyChanged, ICriteriaTreeNode//, IXmlSerializable
	{
		//public event PropertyChangedEventHandler PropertyChanged;
		private WeakEventSource<PropertyChangedEventArgs> _wes_PropertyChanged = new WeakEventSource<PropertyChangedEventArgs>();
		public event PropertyChangedEventHandler PropertyChanged
		{
			add
			{
				if (_wes_PropertyChanged == null)
				{
					_wes_PropertyChanged = new WeakEventSource<PropertyChangedEventArgs>();
				}
				_wes_PropertyChanged.Subscribe(new EventHandler<PropertyChangedEventArgs>(value));
			}
			remove { _wes_PropertyChanged.Unsubscribe(new EventHandler<PropertyChangedEventArgs>(value)); }
		}

        

        #region serialize
        [DataMember(Order = 0)]
		private string _fieldName;
		[DataMember(Order = 1)]
		private string _fieldFullName;
        [DataMember(Order = 2)]
        private bool _isAdvanced;
        [DataMember(Order = 3)]
        private string _advancedCriteriaString
        {
            get
            {
                return !ReferenceEquals(null, _advancedCriteria) ? _advancedCriteria.LegacyToString() : "";
            }
            set
            {
                _advancedCriteria = value == "" ? null : CriteriaOperator.Parse(value);
            }
        }
        #endregion

        // advanced criteria
        private CriteriaOperator _advancedCriteria;

        // hold cached fake filter object
        private object _fakeFilterObject;

		#region ICriteriaTreeNode

		public CriteriaOperator GetCondition() {
            return _isAdvanced ? _advancedCriteria : GetCriteriaOperator();
		}
		public string GetFullFieldName()
		{
			return _fieldFullName;
		}
		public string GetFieldName()
		{
			return _fieldName;
		}
		public void Reset()
		{
            
            GetType()
			.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
			.Select(field => field.GetValue(this))
			.Cast<ICriteriaTreeNode>()
			.ToList()
			.ForEach(c => { c.Reset(); } );

            //invoke property changed on last property, it will force update all bindings anyway
            OnPropertyChanged(String.Empty);

            // advanced
            _isAdvanced = false;
            _advancedCriteria = null;
        }

        #endregion

        public FilterObjectbase() : this("", "") { }

        public FilterObjectbase(string pn, string fn)
        {
            if (_wes_PropertyChanged == null) _wes_PropertyChanged = new WeakEventSource<PropertyChangedEventArgs>();
            _fieldName = fn;
            _fieldFullName = pn != "" ? pn + "." + fn : fn;
            _isAdvanced = false;
            _advancedCriteria = null;
            _fakeFilterObject = null;
        }      

        // retur fake filter object
        public object GetFakeFilterObject()
        {
            if (_fakeFilterObject == null)
            {
                AssemblyBuilder dynamicAssembly =
                    AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("xwcs.dynamic.data.assembly"),
                        AssemblyBuilderAccess.Run);
                ModuleBuilder dynamicModule = dynamicAssembly.DefineDynamicModule("xwcs.dynamic.data.assembly.Module");
                _fakeFilterObject = Activator.CreateInstance(CreateFakeFilterObjectType(GetType().Name, GetType(), dynamicModule));
            }

            return _fakeFilterObject;
        }

        

        // take current condition from fields and save it in criteria string
        public void SaveToAdvancedCriteria()
        {
            _advancedCriteria = GetCriteriaOperator();
        }


        // cant use getter/setter
        public CriteriaOperator GetAdvancedCriteria()
        {
            return _advancedCriteria;
        }
        public void  SetAdvancedCriteria(CriteriaOperator c)
        {
            _advancedCriteria = c;
        }
        public bool GetIsAdvanced()  
        {
            return _isAdvanced;

        }
        public void SetIsAdvanced(bool a)
        {
            _isAdvanced = a;
        }

        public ICriteriaTreeNode GetFilterFieldByPath(string path)
        {
            return GetFilterFieldByPath(this, path);
        }

        public bool HasCriteria()
        {
            return true;
        }

        public void ResetFieldByName(string FieldName)
        {
            GetFilterFieldByPath(FieldName)?.Reset();
            OnPropertyChanged();
        }



        protected void SetField<T>(ref FilterField<T> storage, object value, [CallerMemberName] string propertyName = null)
		{
			MethodBase info = new StackFrame(3).GetMethod();
			Console.WriteLine(string.Format("{0}.{1} -> {2}", info.DeclaringType.Name, info.Name, propertyName));
			storage.Value = (T)value;
			//PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			_wes_PropertyChanged.Raise(this, new PropertyChangedEventArgs(propertyName));
		}

		protected void SetFieldCriteria<T>(ref FilterField<T> storage, CriteriaOperator value, [CallerMemberName] string propertyName = null)
		{
			var oldValue = storage.Value;	
			storage.Condition = value;
			//condition set value to null, so if old was something else notify property change
			if(oldValue != null)
				//PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
				_wes_PropertyChanged.Raise(this, new PropertyChangedEventArgs(propertyName)); //value set to null
		}

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			//PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			_wes_PropertyChanged.Raise(this, new PropertyChangedEventArgs(propertyName));
		}

		

		private ICriteriaTreeNode GetFilterFieldByPath(ICriteriaTreeNode obj, string path) {
			if (obj == null) { return null; }

			int l = path.IndexOf(".");
			string name = path;
			string suffix = "";
			if (l > 0)
			{
				name = path.Substring(0, l);
				suffix = path.Substring(l + 1);
			}

			try {
				obj = obj.GetType()
				.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
				.Where(field => field.Name == '_' + name)
				.Select(field => field.GetValue(obj))
				.Single() as ICriteriaTreeNode;
			}catch(Exception) {
				return null;
			}			
			
			//not done			
			if(suffix != "") {
				return GetFilterFieldByPath(obj, suffix);
			}

			//done
			return obj;
		}

        private CriteriaOperator GetCriteriaOperator()
        {
            //build condition
            return CriteriaOperator.And(
                    this.GetType()
                    .GetFields(System.Reflection.BindingFlags.NonPublic | BindingFlags.Instance)
                    .Select(field => field.GetValue(this))
                    .Cast<ICriteriaTreeNode>()
                    .Select(c => c.GetCondition())
                    .AsEnumerable()
            );
        }


        // make filter fake object
        private Type CreateFakeFilterObjectType(string name, Type t, ModuleBuilder mb = null)
        {
            // make first type from all fields
            TypeBuilder ntb = mb.DefineType(name + "_t", TypeAttributes.Public);

            // fields   
            IEnumerable<FieldInfo> fields = t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

            fields.Where(f => f.FieldType.IsSubclassOfRawGeneric(typeof(FilterField<>)))
            .Select(f => new fake_filter_field()
            {
                name = f.Name.Substring(f.Name.IndexOf('_') + 1),
                // filter field is template so take its first generic argument and check it if it is generic again
                type = f.FieldType.GenericTypeArguments[0].IsGenericType ?
                            // nullable<>
                            f.FieldType.GenericTypeArguments[0].GenericTypeArguments[0] :
                            // direct type
                            f.FieldType.GenericTypeArguments[0]
            }).ToList()
            .ForEach(c => { ReflectionHelper.AddProperty(ntb, c.name, c.type); });

            // nested collections
            fields.Where(f => f.FieldType.IsSubclassOfRawGeneric(typeof(FilterObjectsCollection<>)))
            .Select(f => new fake_filter_field()
            {
                name = f.Name.Substring(f.Name.IndexOf('_') + 1),
                type = f.FieldType.GenericTypeArguments[0]
            }).ToList()
            .ForEach(c =>
            {
                ReflectionHelper.AddProperty(
                    ntb,
                    c.name,
                    typeof(List<>).MakeGenericType(new Type[] { CreateFakeFilterObjectType(c.name, c.type, mb) })
                );
            });

            // nested objects
            fields.Where(f => f.FieldType.IsSubclassOfRawGeneric(typeof(FilterObjectbase)))
            .Select(f => new fake_filter_field()
            {
                name = f.Name.Substring(f.Name.IndexOf('_') + 1),
                type = f.FieldType
            }).ToList()
            .ForEach(c =>
            {
                ReflectionHelper.AddProperty(
                    ntb,
                    c.name,
                    CreateFakeFilterObjectType(c.name, c.type, mb)
                );
            });

            // now make property
            return ntb.CreateType();
        }

    }
}
