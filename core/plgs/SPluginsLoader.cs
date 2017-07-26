using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Linq;

namespace xwcs.core.plgs
{
    public class SPluginsLoader
    {
        private static SPluginsLoader instance;
		private static Dictionary<string, Type> typeCache = null;

		//singleton need private ctor
		private SPluginsLoader()
        {
			typeCache = new Dictionary<string, Type>();
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

		public bool TryFindType(string typeName, out Type t)
		{
			t = null;
			if (typeName == "") return false;
            lock (typeCache)
			{
				if (!typeCache.TryGetValue(typeName, out t))
				{
					foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
					{
						t = a.GetType(typeName);
						if (t != null)
							break;
					}
					typeCache[typeName] = t; // perhaps null
				}
			}
			return t != null;
		}

		/****

            MAIN methods
        */

		private Dictionary<Guid, IPlugin> _plugins = new Dictionary<Guid, IPlugin>();

        public void LoadPlugins(IPluginHost host, string path)
        {
            List<string> dllFileNames = new List<string>();

            if (Directory.Exists(path))
            {
                dllFileNames.AddRange(Directory.GetFiles(path, "plugin.*.dll"));
				dllFileNames.AddRange(Directory.GetFiles(path, "*.plugin.*.dll"));

				List<Assembly> assemblies = new List<Assembly>();
                foreach (string dllFile in dllFileNames)
                {
                    Assembly assembly = AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(dllFile)); //this is need to have Singletons shared!!!!
                    assemblies.Add(assembly);
                }

                Type pluginType = typeof(IPlugin);
                List<Type> pluginTypes = new List<Type>();
                foreach (Assembly assembly in assemblies)
                {
                    if (assembly != null)
                    {
                        string nameSpace = getNamespace(assembly);
                        Type typeInfo = assembly.GetType(nameSpace + ".AssemblyInfo");
                        if (typeInfo != null)
                        {
                            IAssemblyInfo info = (IAssemblyInfo)Activator.CreateInstance(typeInfo);
                            foreach(string name in info.Plugins)
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

		public IPlugin getPluginByName(string n)
		{
			IEnumerable<IPlugin> p = _plugins.Values.Where(o => o.Info.Name == n);
			if(p.Count() > 0) {
				return p.First();
			}
			return null;
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


        public void Unload()
        {
            foreach(IPlugin p in _plugins.Values)
            {
                p.Dispose();
            }
        }

    }
}
