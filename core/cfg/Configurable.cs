using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace xwcs.core.cfg
{
    /// <summary>
    /// Base class for all configurable classes
    /// </summary>
    public class Configurable
    {
        private string _configName;
        private XmlNode _cfg;

        /// <summary>
        /// Ctor
        /// </summary>
        protected Configurable()
        {
            //init eventual configuration
            var attr = this.GetType().GetCustomAttributes(typeof(attr.Config), false).FirstOrDefault() as attr.Config;
            _configName = attr != null ? attr.name : this.GetType().Name;
            _cfg = ConfigData.Open().getConfig(_configName);
        }

        /// <summary>
        /// ctro from name
        /// </summary>
        /// <param name="name"></param>
        protected Configurable(string name)
        {
            _configName = name;
            _cfg = ConfigData.Open().getConfig(_configName);
            
        }

       
        /// <summary>
        /// return cfg param
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defVal"></param>
        /// <returns></returns>
        public string getCfgParam(string key, string defVal = "")
        {
            XmlNode n = _cfg.SelectSingleNode(key);

            if (n != null)
            {
                return n.InnerText;
            }

            return defVal;
        }

        /// <summary>
        /// Return configuration param node for eventual modify
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public XmlNode getCfgParamNode(string key)
        {
            XmlNode n = _cfg.SelectSingleNode(key);

            if (n == null)
            {
                //add one
                n = _cfg.AppendChild(_cfg.OwnerDocument.CreateElement(key));
            }

            return n;
        }

        /// <summary>
        /// config name accessor
        /// </summary>
        public string configName
        {
            get
            {
                return _configName;
            }
        }
    }
}
