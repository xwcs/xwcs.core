using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace xwcs.core
{
    /*
    public enum IterType : System.Int32
    {
        Normativa = 1 << 0,
        Prassi = 1 << 1,
        Prassi_amministrativa = 1 << 1,
        Giurisprudenza = 1 << 2,
        Agenzia = 1 << 3,
        Quesito = 1 << 4,
        Bibliografia = 1 << 5,
        Inpratica = 1 << 6,
        Violazioni = 1 << 7,
        All = 0x000000ff
    }
    */

    /// <summary>
    /// Wrap T type enum in class with some extended functionalities
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ExtendedEnum
    {
        private static Dictionary<Type, Func<string, object>> _parsers = new Dictionary<Type, Func<string, object>>();
        private static Func<string, object> GetParseEnumDelegate(Type tEnum)
        {
            var eValue = Expression.Parameter(typeof(string), "value"); // (String value)
            var tReturn = typeof(Object);

            Expression<Func<string, object>> l = Expression.Lambda<Func<string, object>>(
                Expression.Block(tReturn,
                  Expression.Convert( // We need to box the result (tEnum -> Object)
                    Expression.Switch(tEnum, eValue,
                      /*  
                      Expression.Block(tEnum,
                        Expression.Throw(Expression.New(typeof(Exception).GetConstructor(Type.EmptyTypes))),
                        Expression.Default(tEnum)
                      ),
                      */
                      Expression.Default(tEnum),
                      null,
                      Enum.GetValues(tEnum).Cast<object>().Select(v => Expression.SwitchCase(
                        Expression.Constant(v),
                        Expression.Constant(v.ToString().ToLower())
                      )).ToArray()
                    ), tReturn
                  )
                ), eValue
              );

            return  l.Compile();
        }

        public static int ToInt(Type enumType, object value)
        {
            if (ReferenceEquals(value, null) || value.ToString() == "") return 0x000000ff;

            Func<string, object> _parser = null;

            if (!_parsers.ContainsKey(enumType))
            {
                // make parser
                _parser = GetParseEnumDelegate(enumType);

                /*
                _parser = (string vv) =>
                {

                    switch (vv)
                    {
                        case ("normativa"):
                            return (object)IterType.Normativa;
                        case ("prassi"):
                            return (object)IterType.Prassi;
                        case ("prassi_amministrativa"):
                            return (object)IterType.Prassi;
                        case ("giurisprudenza"):
                            return (object)IterType.Giurisprudenza;
                        case ("agenzia"):
                            return (object)IterType.Agenzia;
                        case ("quesito"):
                            return (object)IterType.Quesito;
                        case ("bibliografia"):
                            return (object)IterType.Bibliografia;
                        case ("inpratica"):
                            return (object)IterType.Inpratica;
                        case ("violazioni"):
                            return (object)IterType.Violazioni;
                        case ("all"):
                            return (object)IterType.All;
                        default:
                            return (object)IterType.All;
                    }
                };
                */

            _parsers[enumType] = _parser;
            }
            else
            {
                _parser = _parsers[enumType];
            }
            
            return (int)_parser(value.ToString().ToLower());
        }
    }

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
