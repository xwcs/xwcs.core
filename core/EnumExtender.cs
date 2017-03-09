using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xwcs.core
{
    public static class EnumExtender
    {
        /// <summary>
        /// Adds a flag value to enum.
        /// Please note that enums are value types so you need to handle the RETURNED value from this method.
        /// Example: myEnumVariable = myEnumVariable.AddFlag(CustomEnumType.Value1);
        /// </summary>
        public static T Add<T>(this Enum type, T enumFlag)
        {
            try
            {
                return (T)(object)((int)(object)type | (int)(object)enumFlag);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(string.Format("Could not append flag value {0} to enum {1}", enumFlag, typeof(T).Name), ex);
            }
        }

        /// <summary>
        /// Removes the flag value from enum.
        /// Please note that enums are value types so you need to handle the RETURNED value from this method.
        /// Example: myEnumVariable = myEnumVariable.RemoveFlag(CustomEnumType.Value1);
        /// </summary>
        public static T Remove<T>(this Enum type, T enumFlag)
        {
            try
            {
                return (T)(object)((int)(object)type & ~(int)(object)enumFlag);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(string.Format("Could not remove flag value {0} from enum {1}", enumFlag, typeof(T).Name), ex);
            }
        }

        /// <summary>
        /// Sets flag state on enum.
        /// Please note that enums are value types so you need to handle the RETURNED value from this method.
        /// Example: myEnumVariable = myEnumVariable.SetFlag(CustomEnumType.Value1, true);
        /// </summary>
        public static T Set<T>(this Enum type, T enumFlag, bool value)
        {
            return value ? type.Add(enumFlag) : type.Remove(enumFlag);
        }

        /// <summary>
        /// Checks if the flag value is identical to the provided enum.
        /// </summary>
        public static bool Is<T>(this Enum type, T enumFlag)
        {
            try
            {
                return (int)(object)type == (int)(object)enumFlag;
            }
            catch
            {
                return false;
            }
        }

        public static bool Has<T>(this Enum type, T value)
        {
            try
            {
                return (((int)(object)type & (int)(object)value) == (int)(object)value);
            }
            catch
            {
                return false;
            }
        }
    }
}
