namespace xwcs.core.db.model
{
    using System;
	using System.Linq;
	using System.Collections.Generic;
    using System.ComponentModel;
    using System.Security.Permissions;
	using System.ComponentModel.DataAnnotations;


	public sealed class HyperTypeDescriptionProvider : TypeDescriptionProvider
    {
		private manager.ILogger _logger;

		public static void MorphPropertyType(object obj, string propName)
		{
			try
			{
				Type objectType = obj.GetType();
				PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(objectType);
				ChainingPropertyDescriptor pd = pdc.Find(propName, false) as ChainingPropertyDescriptor;
				if (pd != null)
				{
					object val = pd.GetValue(obj);
                    pd.ForcedPropertyType = val.GetType();
				}

				//now for EF proxy too
				if (objectType.BaseType != null && objectType.Namespace == "System.Data.Entity.DynamicProxies")
				{
					objectType = objectType.BaseType;

					pdc = TypeDescriptor.GetProperties(objectType);
					pd = pdc.Find(propName, false) as ChainingPropertyDescriptor;
					if (pd != null)
					{
						pd.ForcedPropertyType = pd.GetValue(obj).GetType();
					}
				}

				
			}
			catch (Exception) { }

		}

		private static Dictionary<int, Type> _registeredTypes = new Dictionary<int, Type>();

		public static void Add(Type type)
        {
			//Avoid some base types!!!!!!
			if (type.FullName == "System.Object" ||
				type.FullName == "System.String" ||
				type.FullName == "System.DateTime" ||
				type.IsPrimitive ||
				type.IsValueType ||
                !type.IsClass
				)
				return;
			int hc = type.GetHashCode();
			bool doRegister = false;
			lock (_registeredTypes) {
				if(!_registeredTypes.ContainsKey(hc)) {
					_registeredTypes.Add(hc, type);
					doRegister = true;
				}
			}
			if(doRegister) {
				TypeDescriptionProvider parent = TypeDescriptor.GetProvider(type);
				TypeDescriptor.AddProvider(new HyperTypeDescriptionProvider(parent), type);
			}
        }

        public HyperTypeDescriptionProvider() : this(typeof (object))
        {
			_logger = manager.SLogManager.getInstance().getClassLogger(GetType());
		}

        public HyperTypeDescriptionProvider(Type type) : this(TypeDescriptor.GetProvider(type))
        {
			_logger = manager.SLogManager.getInstance().getClassLogger(GetType());
		}

        public HyperTypeDescriptionProvider(TypeDescriptionProvider parent) : base(parent)
        {
			_logger = manager.SLogManager.getInstance().getClassLogger(GetType());
		}

        public static void Clear(Type type)
        {
            lock (descriptors)
            {
                descriptors.Remove(type);
            }
        }

        public static void Clear()
        {
            lock (descriptors)
            {
                descriptors.Clear();
            }
        }

        private static readonly Dictionary<Type, ICustomTypeDescriptor> descriptors =
            new Dictionary<Type, ICustomTypeDescriptor>();

        public override sealed ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
        {
			lock (descriptors)
            {
                ICustomTypeDescriptor descriptor;
                if (!descriptors.TryGetValue(objectType, out descriptor))
                {
                    try
                    {
                        descriptor = BuildDescriptor(objectType);
                    }
                    catch
                    {
                        return base.GetTypeDescriptor(objectType, instance);
                    }
                }
                return descriptor;
            }
        }

		[ReflectionPermission(SecurityAction.Assert, Unrestricted = true)]
        private ICustomTypeDescriptor BuildDescriptor(Type objectType)
        {
            // NOTE: "descriptors" already locked here

            // get the parent descriptor and add to the dictionary so that
            // building the new descriptor will use the base rather than recursing
            ICustomTypeDescriptor descriptor = base.GetTypeDescriptor(objectType, null);
            descriptors.Add(objectType, descriptor);
            try
            {
                // build a new descriptor from this, and replace the lookup
                descriptor = new HyperTypeDescriptor(descriptor, objectType);
                descriptors[objectType] = descriptor;
                return descriptor;
            }
            catch
            {
                // rollback and throw
                // (perhaps because the specific caller lacked permissions;
                // another caller may be successful)
                descriptors.Remove(objectType);
                throw;
            }
        }
	}
}