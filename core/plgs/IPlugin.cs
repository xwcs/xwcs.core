namespace xwcs.core.plgs
{
    public interface IPlugin
    {
        void createPluginInfo(string name, string version, pluginType type);
        PluginInfo Info { get; }
        void init(/*IPluginHost host*/);
    }
}

