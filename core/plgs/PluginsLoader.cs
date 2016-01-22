﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace xwcs.core.plgs
{
    public class PluginsLoader
    {
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

                    Dictionary<string, Guid> controls = plugin.Info.Controls;
                    foreach(Guid guid in controls.Values)
                    {
                        _plugins.Add(guid, plugin);
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

    }
}
