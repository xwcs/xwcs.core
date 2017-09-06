using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xwcs.core.db
{
    public class TypeCacheData
    {
        public Dictionary<string, MethodInfo> Getters = new Dictionary<string, MethodInfo>();
        public Dictionary<string, MethodInfo> Setters = new Dictionary<string, MethodInfo>();
        public Dictionary<string, PropertyInfo> Properties = new Dictionary<string, PropertyInfo>();
        public Dictionary<Type, IEnumerable<PropertyInfo>> PropertiesForAttribCache = new Dictionary<Type, IEnumerable<PropertyInfo>>();
    }

    public static class TypeCache
    {
        public static Dictionary<Type, TypeCacheData> _cache = new Dictionary<Type, TypeCacheData>();

        public static TypeCacheData GetTypeCacheData(Type t)
        {
            TypeCacheData ret;
            if (!_cache.TryGetValue(t, out ret))
            {
                ret = new TypeCacheData();
                _cache.Add(t, ret);
            }

            return ret;
        }
    }
}
