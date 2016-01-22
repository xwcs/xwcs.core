using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xwcs.core.evt;

namespace xwcs.core.plgs
{
    public abstract class PluginBase : IPlugin
    {

        private PluginInfo _pluginInfo;
        private xwcs.core.evt.SEventProxy _eventProxy;


        public void createPluginInfo(string name, string version, pluginType type)
        {
            _pluginInfo = new PluginInfo(name, version, type);
        }

        public PluginInfo Info
        {
            get { return _pluginInfo; }
        }

        public SEventProxy EventProxy
        {
            get
            {
                return _eventProxy;
            }
        }

        protected void setup()
        {
            //get proxy, so we are sure it exists
            _eventProxy = xwcs.core.evt.SEventProxy.getInstance();
        }

        abstract public void init();
    }
}
