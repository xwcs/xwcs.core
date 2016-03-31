namespace xwcs.core.db.model
{
    using System;
    using System.ComponentModel;
    using System.Linq;


	public static class TypeExtensionMethods
	{
        public static Y MapTo<T, Y>(this T input) where Y : class, new()
        {
            Y output = new Y();
            var propsT = typeof(T).GetProperties();
            var propsY = typeof(Y).GetProperties();

            var similarsT = propsT.Where(x => 
                          propsY.Any(y => y.Name == x.Name
                   && y.PropertyType == x.PropertyType)).OrderBy(x => x.Name).ToList();

            var similarsY = propsY.Where(x =>
                            propsT.Any(y => y.Name == x.Name
                    && y.PropertyType == x.PropertyType)).OrderBy(x => x.Name).ToList();

            for (int i = 0; i < similarsY.Count; i++)
            {
                similarsY[i]
                .SetValue(output, similarsT[i].GetValue(input, null), null);
            }

            return output;
        }

        public static void CopyTo<T>(this T input, ref T output) 
        {
            foreach(PropertyDescriptor pd in TypeDescriptor.GetProperties(input))
            {
                pd.SetValue(output, pd.GetValue(input));
            }
        }

        public static void CopyFrom(this object dest, object input)
        {
            foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(dest))
            {
                pd.SetValue(dest, pd.GetValue(input));
            }
        }

        public static object GetPropValueByPathUsingReflection(this object obj, string name)
		{
			foreach (string part in name.Split('.'))
			{
				if (obj == null) { return null; }
				System.Reflection.PropertyInfo info = obj.GetType().GetProperty(part);
				if (info == null) { return null; }

				obj = info.GetValue(obj, null);
			}
			return obj;
		}

		public static void SetPropValueByPathUsingReflection(this object obj, string name, object value)
		{
			try
			{
				object lastObject = null;

				System.Reflection.PropertyInfo info = null;
				foreach (string part in name.Split('.'))
				{
					if (obj == null) { return; }
					//get info of property connected to current obj
					info = obj.GetType().GetProperty(part);
					if (info == null){ return; }
					//go deeper
					lastObject = obj;
					obj = info.GetValue(obj, null);
				}
				//we are at the end so set value
				info.SetValue(lastObject, value, null);
			}
			catch (Exception) {
				throw new InvalidEnumArgumentException();
			}			
		}

		/*	
		public static T GetPropValueByPath<T>(this object obj, string name)
		{
			object retval = GetPropValueByPath(obj, name);
			if (retval == null) { return default(T); }

			// throws InvalidCastException if types are incompatible
			return (T)retval;
		}
		*/
	}

	public abstract class ChainingPropertyDescriptor : PropertyDescriptor
    {
        private readonly PropertyDescriptor _root;

		//make possibility to force type externally
		private Type _forcedType = null;


		protected PropertyDescriptor Root
        {
            get { return _root; }
        }

        protected ChainingPropertyDescriptor(PropertyDescriptor root)
            : base(root)
        {
            _root = root;
        }

        public override void AddValueChanged(object component, EventHandler handler)
        {
            Root.AddValueChanged(component, handler);
        }

        public override AttributeCollection Attributes
        {
            get { return Root.Attributes; }
        }

        public override bool CanResetValue(object component)
        {
            return Root.CanResetValue(component);
        }

        public override string Category
        {
            get { return Root.Category; }
        }

        public override Type ComponentType
        {
            get { return Root.ComponentType; }
        }

        public override TypeConverter Converter
        {
            get { return Root.Converter; }
        }

        public override string Description
        {
            get { return Root.Description; }
        }

        public override bool DesignTimeOnly
        {
            get { return Root.DesignTimeOnly; }
        }

        public override string DisplayName
        {
            get { return Root.DisplayName; }
        }

        public override bool Equals(object obj)
        {
            return Root.Equals(obj);
        }

        public override PropertyDescriptorCollection GetChildProperties(object instance, Attribute[] filter)
        {
            return Root.GetChildProperties(instance, filter);
        }

        public override object GetEditor(Type editorBaseType)
        {
            return Root.GetEditor(editorBaseType);
        }

        public override int GetHashCode()
        {
            return Root.GetHashCode();
        }

        public override object GetValue(object component)
        {
			return Root.GetValue(component);
        }

        public override bool IsBrowsable
        {
            get { return Root.IsBrowsable; }
        }

        public override bool IsLocalizable
        {
            get { return Root.IsLocalizable; }
        }

        public override bool IsReadOnly
        {
            get { return Root.IsReadOnly; }
        }

        public override string Name
        {
            get { return Root.Name; }
        }

		public override Type PropertyType
		{
			get	{
				//Console.WriteLine("Type:" + (_forcedType == null ? Root.PropertyType : _forcedType).ToString());
				return _forcedType == null ? Root.PropertyType : _forcedType;
			}
		}

		public Type ForcedPropertyType
		{
			get { return _forcedType == null ? Root.PropertyType : _forcedType; }
			set {
				if(_forcedType != value) {
					_forcedType = value;
				}			
			}
		}

		public override void RemoveValueChanged(object component, EventHandler handler)
        {
            Root.RemoveValueChanged(component, handler);
        }

        public override void ResetValue(object component)
        {
            Root.ResetValue(component);
        }

        public override void SetValue(object component, object value)
        {
			Root.SetValue(component, value);
        }

        public override bool ShouldSerializeValue(object component)
        {
            return Root.ShouldSerializeValue(component);
        }

        public override bool SupportsChangeEvents
        {
            get { return Root.SupportsChangeEvents; }
        }

        public override string ToString()
        {
            return Root.ToString();
        }
    }
}