using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace xwcs.core.db
{
    public class TypeCacheData
    {
        private Type _type;

        public Dictionary<string, PropertyInfo> Properties = new Dictionary<string, PropertyInfo>();
        public Dictionary<Type, IEnumerable<PropertyInfo>> PropertiesForAttribCache = new Dictionary<Type, IEnumerable<PropertyInfo>>();
        public Dictionary<string, IEnumerable<Attribute>> AttributesByPropertyName = new Dictionary<string, IEnumerable<Attribute>>();

        public Dictionary<string, PropertyDescriptor> Pds = new Dictionary<string, PropertyDescriptor>();
        

        public TypeCacheData(Type t)
        {
            _type = t;
            foreach (PropertyInfo pi in t.GetProperties())
            {
                Properties.Add(pi.Name, pi);
            }
            foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(t))
            {
                Pds.Add(pd.Name, pd);
            }
        }

        public IEnumerable<PropertyInfo> GetPropertiesWithAttributeType(Type AttrType)
        {
            IEnumerable<PropertyInfo> pps;

            lock (AttrType)
            {
                if (!PropertiesForAttribCache.TryGetValue(AttrType, out pps))
                {
                    pps = Properties.Values.Where(
                        prop => GetCustomAttributesForProperty(prop.Name).Any(a => a.GetType().IsAssignableFrom(AttrType)) 
                    );

                    /*
                    foreach(PropertyInfo pi in Properties.Values)
                    {
                        List<Attribute> atts = GetCustomAttributesForProperty(pi.Name).ToList();
                        bool r = atts.Any(a => a.GetType().IsAssignableFrom(AttrType));
                    }
                    */
                    PropertiesForAttribCache.Add(AttrType, pps);
                }
            }           

            return pps;
        }

        public IEnumerable<Attribute> GetCustomAttributesForProperty(string propertyName)
        {
            IEnumerable<Attribute> ret;
            if(!AttributesByPropertyName.TryGetValue(propertyName, out ret))
            {
                // capture them 
                ret = Properties[propertyName].GetCustomAttributes().Cast<Attribute>();
                // merge with meta class
                MetadataTypeAttribute l = TypeDescriptor.GetAttributes(_type).OfType<MetadataTypeAttribute>().FirstOrDefault();
                if (!ReferenceEquals(null, l))
                {
                    PropertyInfo pi = l.MetadataClassType.GetProperty(propertyName);
                    if (pi != null)
                    {
                        ret = ret.Union(pi.GetCustomAttributes().Cast<Attribute>());
                    }
                }
                AttributesByPropertyName.Add(propertyName, ret);
            }

            return ret;
        }

    }

    public static class TypeCache
    {
        public static Dictionary<Type, TypeCacheData> _cache = new Dictionary<Type, TypeCacheData>();

        public static TypeCacheData GetTypeCacheData(Type t)
        {
            lock (t)
            {
                TypeCacheData ret;
                if (!_cache.TryGetValue(t, out ret))
                {
                    ret = new TypeCacheData(t);                    

                    _cache.Add(t, ret);
                }
                return ret;
            }
        }
    }
}
