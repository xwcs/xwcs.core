namespace xwcs.core.plgs
{
    public interface IPluginInfo
    {
        string[] Plugins { get; }
    }

    public interface IPlugin
    {
        PluginInfo pluginInfo { get;  }
        void init(IPluginHost host);
    }
}

