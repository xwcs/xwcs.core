using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using xwcs.core.db.model;

namespace xwcs.core.db
{

	static class ExtensionMethods
	{
		/*
			this method serialize some object into xml with full type name in 
			__content_type__="<object type>" attribute
			so we can reread it later and it use also specific root element name
		*/
		public static IStructureWatcher GetStructureWatcher<T>(this T objectInstance)
		{
			return new StructureWatcher<T>();
		}
	}

	/*
	int ret = 5381;
    		Type t;
    					
    		t = _content_dump_xml_obj != null ? _content_dump_xml_obj.GetType() : typeof(object);
    		ret = ((ret << 5) + ret) ^ t.GetHashCode();
	*/

	public interface IStructureWatcher {
		// this method check types for all [Mutable] properties of a target
		// it will return true if state of types changed vs alts call
		bool CheckStructure(SerializedEntityBase target);
	}	

	public class StructureWatcher<T> : IStructureWatcher
	{
		private static Dictionary<string, ChainingPropertyDescriptor> _descriptorsCache = new Dictionary<string, ChainingPropertyDescriptor>();
		
		// if any internal field type change this will grow
		private static int _lastTypeHash = -1;

		private static bool _typeInitialized = false;
		
		public StructureWatcher() {
			//read structure of type
			PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(typeof(T));
			foreach(ChainingPropertyDescriptor pd in pdc.OfType<ChainingPropertyDescriptor>()) {
				if(pd.Attributes.OfType<model.attributes.MutableAttribute>().ToList().Count > 0) {
					//we have one mutable filed
					lock(_descriptorsCache) {
						if (!_descriptorsCache.ContainsKey(pd.Name))
						{
							_descriptorsCache[pd.Name] = pd;
						}
					}					
				}
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


		// if true structure was changed
		public bool CheckStructure(SerializedEntityBase o) {
			// read all mutable attributes if not done
			Dictionary<string, Type> dest = new Dictionary<string, Type>();
			o.GetMutablePropertiesType(dest);
			
			int ret = 5381;
    		foreach(KeyValuePair<string, Type> entry in dest) {
				ret = ((ret << 5) + ret) ^ entry.Value.GetHashCode();
				_descriptorsCache[entry.Key].ForcedPropertyType = entry.Value;	
			}
    		
			if(_lastTypeHash != ret) {
				_lastTypeHash = ret;

				return true;
			}
	
			return false;
		}
	}
}
