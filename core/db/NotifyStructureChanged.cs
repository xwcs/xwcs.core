using System;
using System.Collections.Generic;
using System.ComponentModel;
using xwcs.core.db.model;

namespace xwcs.core.db
{
	public class StructureChangedEventArgs : EventArgs
	{
		public StructureChangedEventArgs(Type t, ChainingPropertyDescriptor pd) {
			SourceType = t;
			PropertyDescriptor = pd;
		}
		public Type SourceType { get; private set; }
		public ChainingPropertyDescriptor PropertyDescriptor{ get; private set; }
	}

	public delegate void StructureChangedEventHandler<T>(NotifyStructureChanged<T> sender, StructureChangedEventArgs e);

	public abstract class NotifyStructureChanged<T>
	{
		public static event StructureChangedEventHandler<T> StructureChanged;

		private static Dictionary<string, ChainingPropertyDescriptor> _descriptorsCache = new Dictionary<string, ChainingPropertyDescriptor>();
		protected static Dictionary<string, Type> _currentPropsTypes = new Dictionary<string, Type>();

		// if any internal field type change this will grow
		private static Int64 _internalTypesVersion = 0;

		public static Int64 InternalTypesVersion { get; }

		private static bool _typeDone = false;

		protected NotifyStructureChanged() {
			if(!_typeDone) {
				// handle type registering
				// we have to unbox from eventual entity proxy
				Type t = GetType();

				if (t.BaseType != null && t.Namespace == "System.Data.Entity.DynamicProxies")
				{
					HyperTypeDescriptionProvider.Add(t.BaseType);
				}
				else
				{
					HyperTypeDescriptionProvider.Add(t);
				}
				_typeDone = true;
			}
		}

		public ChainingPropertyDescriptor GetPropertyDescriptor(string PropertyName)
		{
			lock (_descriptorsCache)
			{
				ChainingPropertyDescriptor pd;
				if (!_descriptorsCache.TryGetValue(PropertyName, out pd))
				{
					PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(this.GetType());
					pd = (ChainingPropertyDescriptor)pdc.Find(PropertyName, false);
					if (pd != null)
					{
						_descriptorsCache[PropertyName] = pd;
						return pd;
					}
					throw new FieldAccessException(string.Format("Field {0} has no ChainingPropertyDescriptor!", PropertyName));
				}
				return pd;
			}
		}

		protected void CheckType(object o, string propertyName) {
			Type t = null, nt = o != null ? o.GetType() : typeof(object);
			if (!_currentPropsTypes.TryGetValue(propertyName, out t))
			{
				_currentPropsTypes[propertyName] = nt;
			}
			if (nt != t)
			{
				++_internalTypesVersion;
				_currentPropsTypes[propertyName] = nt;
				//add type
				HyperTypeDescriptionProvider.Add(nt);
				ChainingPropertyDescriptor pd = GetPropertyDescriptor(propertyName);
				pd.ForcedPropertyType = nt;
				OnStructureChanged(propertyName, pd);

			}
		}

		protected void OnStructureChanged(string propertyName, ChainingPropertyDescriptor pd)
		{
			var eventHandler = StructureChanged;
			if (eventHandler != null)
			{
				
				eventHandler(this, new StructureChangedEventArgs(this.GetType(), pd));
			}
		}

	}
}
