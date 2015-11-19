using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xwcs.core.cfg.attr
{
    /// <summary>
    /// Config annotation
    ///     it should be used on any configurable class
    ///     it connect specific class to some config sub section
    ///     example:
    ///     <code>
    ///            [Config("TestConfig")]
    ///            class Test : xwcs.core.cfg.Configurable{}
    ///     </code>
    /// </summary>
    public class Config : System.Attribute
    {
        private string _name;

        /// <summary>
        /// name
        /// </summary>
        public string name
        {
            get
            {
                return _name;
            }
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="n"></param>
        public Config(string n)
        {
            _name = n;
        }
    }
}
