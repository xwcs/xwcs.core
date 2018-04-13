using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace xwcs.core
{
    public static class CollectionExtender
    {
        public static bool ContainsKeyPattern<T>(this Dictionary<string, T> nodes, string search)
        {
            return nodes.Keys.Any(k => Regex.Match(search, k).Success);
        }

        public static T GetFirstItemByKeyPattern<T>(this Dictionary<string, T> dict, string search)
        {
            return
                (from p in dict
                 where Regex.Match(search, p.Key).Success
                 select p.Value)
                .First();
        }

        private static bool t(string a, string b)
        {
            return new Regex(a).Match(b).Success;
        }

        public static IList<T> GetItemsByKeyPattern<T>(this Dictionary<string, T> dict, string search)
        {
            return dict.Where(e => t(search, e.Key)).Select(e => e.Value).ToList();
        }

        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int size)
        {
            T[] bucket = null;
            var count = 0;

            foreach (var item in source)
            {
                if (bucket == null)
                    bucket = new T[size];


                bucket[count++] = item;

                if (count != size)
                    continue;

                yield return bucket;

                bucket = null;
                count = 0;
            }

            if (bucket != null && count > 0)
                yield return bucket.Take(count);
        }


        // this extension produce Enumerable of contiguous intervals inside list of numeric values 
        public class NumericInterval<T>
        {
            public T min;
            public T max;
            public override string ToString()
            {
                return string.Format("({0}, {1})", min, max);
            }
        }
        public static IEnumerable<NumericInterval<T>> Intervals<T>(this IEnumerable<T> source)
        {
            NumericInterval<T> bucket = null;
            dynamic current = 0;

            foreach (var item in source)
            {
                if (bucket == null)
                {
                    bucket = new NumericInterval<T>();
                    bucket.min = item;
                    current = item;
                }
                else
                {
                    if(current + 1 == (dynamic)item)
                    {
                        ++current;
                        continue;
                    }else
                    {
                        // end of interval
                        bucket.max = (T)current;

                        // interval is done
                        yield return bucket;

                        // current item goes in new bucket
                        bucket = new NumericInterval<T>();
                        bucket.min = item;
                        current = item;
                    }
                }                
            }

            if (bucket != null)
            {
                bucket.max = (T)current;
                yield return bucket;
            }                
        }
    }
}
