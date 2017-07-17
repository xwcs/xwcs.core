using System;
using System.Drawing;
using System.Resources;

namespace xwcs.core.plgs
{
    public interface IPlugin
    {
        void createPluginInfo(Type pt, string version, PluginKind kind);
        PluginInfo Info { get; }
        void init(/*IPluginHost host*/);
        ResourceManager RsMan { get; }
        System.Globalization.CultureInfo RsManCulture { get; }

        Bitmap getBitmapFromFile(string fileName);
        Icon getIconFromFile(string fileName);
    }
}

