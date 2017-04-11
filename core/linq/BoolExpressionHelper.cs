using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xwcs.core.linq
{
    public class BoolExpressionHelper
    {
        static xwcs.native.boolexpr.Helper helper = new xwcs.native.boolexpr.Helper();

        public static string ToDNF(string expr)
        {
            return helper.ToDNF(expr);
        }
    }
}
