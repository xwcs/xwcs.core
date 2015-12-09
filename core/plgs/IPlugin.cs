using DevExpress.XtraEditors;
using DevExpress.XtraBars;

namespace xwcs.core.plgs
{
    public interface IPlugin
    {
        string name { get; }
        int type { get; }
        string guid { get;  }

        DevExpress.XtraEditors.XtraUserControl pluginControl { get; }
        //DevExpress.XtraBars.BarButtonItem getButton();
        void init(IPluginHost host);


        //Just for test
        void testFireEvent();
    }
}

/*
namespace xwcs.core.plgs
{
    public interface IPlugin
    {
        PluginInfo pluginInfo { get;  }
        void init(IPluginHost host);
    }

    public interface IVisualPlugin : IPlugin
    {
        public DevExpress.XtraEditors.XtraUserControl pluginControl { get; }
    }

    public interface INonVisualPlugin : IPlugin
    {

    }
}
*/
