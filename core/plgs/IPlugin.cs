using System.Drawing;
using System.Resources;

namespace xwcs.core.plgs
{
    public interface IPlugin
    {
        void createPluginInfo(string name, string version, pluginType type);
        PluginInfo Info { get; }
        void init(/*IPluginHost host*/);
        ResourceManager RsMan { get; }
        System.Globalization.CultureInfo RsManCulture { get; }

        Bitmap getBitmapFromFile(string fileName, bool global = false);
        Icon getIconFromFile(string fileName, bool global = false);
    }
}

