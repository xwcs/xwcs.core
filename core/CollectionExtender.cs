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
    }
}
