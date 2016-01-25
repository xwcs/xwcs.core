using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace xwcs.core.plgs
{
    public class SPluginsLoader
    {
        private static SPluginsLoader instance;

        //singleton need private ctor
        private SPluginsLoader()
        {
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static SPluginsLoader getInstance()
        {
            if (instance == null)
            {
                instance = new SPluginsLoader();
            }
            return instance;
        }


        /****

            MAIN methods
        */

        private Dictionary<Guid, IPlugin> _plugins = new Dictionary<Guid, IPlugin>();

        public void LoadPlugins(IPluginHost host, string path)
        {
            string[] dllFileNames = null;

            if (Directory.Exists(path))
            {
                dllFileNames = Directory.GetFiles(path, "*.dll");

                ICollection<Assembly> assemblies = new List<Assembly>(dllFileNames.Length);
                foreach (string dllFile in dllFileNames)
                {
                    AssemblyName an = AssemblyName.GetAssemblyName(dllFile);
                    //Assembly assembly = Assembly.Load(an);
                    Assembly assembly = AppDomain.CurrentDomain.Load(an); //this is need to have Singletons shared!!!!
                    assemblies.Add(assembly);
                }

                Type pluginType = typeof(IPlugin);
                ICollection<Type> pluginTypes = new List<Type>();
                foreach (Assembly assembly in assemblies)
                {
                    if (assembly != null)
                    {
                        string nameSpace = getNamespace(assembly);
                        Type typeInfo = assembly.GetType(nameSpace + ".AssemblyInfo");
                        if (typeInfo != null)
                        {
                            IAssemblyInfo info = (IAssemblyInfo)Activator.CreateInstance(typeInfo);
                            string[] plugins = info.Plugins;
                            foreach(string name in plugins)
                            {
                                Type typePlugin = assembly.GetType(name);
                                if (typePlugin != null)
                                {
                                    pluginTypes.Add(typePlugin);
                                }
                            }
                        }
                    }
                }

                foreach (Type type in pluginTypes)
                {
                    IPlugin plugin = (IPlugin)Activator.CreateInstance(type);
                    plugin.init();

                    Dictionary<Guid, xwcs.core.controls.VisualControlInfo> controls = plugin.Info.Controls;

                    //Very important: one plugin can have more guids (controls). Thatsway we have dictionary(std:map) : guid->plugin. It means more guid may handle to one plugin !!!
                    foreach (xwcs.core.controls.VisualControlInfo info in controls.Values)
                    {
                        _plugins.Add(info.GUID, plugin);
                    }
                }
            }
        }

        public string getNamespace(Assembly assembly)
        {
            string[] ar = assembly.FullName.Split(',');
            if (ar.Length > 0)
            {
                return ar[0];
            }
            return string.Empty;
        }

        
        public IPlugin getPluginByGuid(Guid guid)
        {
            if (_plugins.ContainsKey(guid))
            {
                IPlugin plugin = _plugins[guid];
                if (plugin != null) return plugin;
            }

            return null;
        }
        

        public XtraUserControl getControlByGuid(Guid guid)
        {
            foreach(IVisualPlugin plugin in _plugins.Values)
            {
                XtraUserControl control = plugin.getControlByGuid(guid);
                if (control != null) return control;
            }
            return null;
        }

    }
}
