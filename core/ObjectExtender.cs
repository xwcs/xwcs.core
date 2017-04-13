using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using xwcs.core.manager;

namespace xwcs.core
{
    public static class ObjectExtender
    {
        public static bool TryGetInterfaceGenericParameters(this Type type, Type @interface, out Type[] typeParameters)
        {
            typeParameters = null;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == @interface)
            {
                typeParameters = type.GetGenericArguments();
                return true;
            }

            var implements = type.FindInterfaces((ty, obj) => ty.IsGenericType && ty.GetGenericTypeDefinition() == @interface, null).FirstOrDefault();
            if (implements == null)
                return false;

            typeParameters = implements.GetGenericArguments();
            return true;
        }


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
            foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(input))
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

        public static object GetPropertyByName<T>(this T obj, string name) where T : class
        {
            return obj.GetType().GetProperty(name).GetValue(obj, null);
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
                    if (info == null) { return; }
                    //go deeper
                    lastObject = obj;
                    obj = info.GetValue(obj, null);
                }
                //we are at the end so set value
                info.SetValue(lastObject, value, null);
            }
            catch (Exception)
            {
                throw new InvalidEnumArgumentException();
            }
        }

        public static T CreateDelegate<T>(this MethodInfo method) where T : class
        {
            return Delegate.CreateDelegate(typeof(T), method) as T;
        }


        /// <summary>
        /// Alternative version of <see cref="Type.IsSubclassOf"/> that supports raw generic types (generic types without
        /// any type parameters).
        /// </summary>
        /// <param name="baseType">The base type class for which the check is made.</param>
        /// <param name="toCheck">To type to determine for whether it derives from <paramref name="baseType"/>.</param>
        public static bool IsSubclassOfRawGeneric(this Type toCheck, Type baseType)
        {
            while (toCheck != typeof(object))
            {
                Type cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (baseType == cur)
                {
                    return true;
                }

                toCheck = toCheck.BaseType;
            }

            return false;
        }

        public static bool IsSubclassOfRawGeneric(this FieldInfo toCheckFo, Type baseType)
        {
            Type toCheck = toCheckFo.FieldType;

            while (toCheck != typeof(object))
            {
                Type cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (baseType == cur)
                {
                    return true;
                }

                toCheck = toCheck.BaseType;
            }

            return false;
        }

        /// <summary>
        /// Clone serializable object, 
        /// it will clone object by serializing it in ram, 
        /// and de-serializing it back.
        /// </summary>
        /// <typeparam name="T">Object Type</typeparam>
        /// <param name="what">Object to be cloned</param>
        /// <returns>T typed clone of object</returns>
        public static T CloneSerializableObject<T>(this T what)
        {
            try
            {
                NetDataContractSerializer serial = new NetDataContractSerializer();
                MemoryStream strm = new MemoryStream();
                serial.WriteObject(strm, what);
                strm.Seek(0, SeekOrigin.Begin);
                return (T)serial.ReadObject(strm);
            }
            catch (Exception e)
            {
                SLogManager.getInstance().getClassLogger(typeof(T)).Error(e.Message);
            }

            return default(T);
        }
    }
}
