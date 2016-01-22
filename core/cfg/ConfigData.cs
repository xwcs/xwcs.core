using System;
using System.Configuration;
using System.Xml;
using System.IO;

using Microsoft.Win32;

namespace xwcs.core.cfg
{
    /// <summary>
    /// Configuration data singleton
    /// </summary>
    public class ConfigData : ConfigurationSection
    {
        private ConfigData() {
            _configs = null;
        }

        #region Public Methods

        ///<summary>Get this configuration set from the application's default config file</summary>
        public static ConfigData Open()
        {
            if (instance != null)
            {
                return instance;
            }
            //try registry value
            RegistryKey key = Registry.CurrentUser;
            RegistryKey my = key.OpenSubKey("Software\\3DInformatica\\TestEgaf");
            if (my != null)
            {
                //MessageBox.Show("R:" + (String)(my.GetValue("Config") ?? ""));
                return Open((String)(my.GetValue("Config") ?? ""));
            }
            //assembly
            System.Reflection.Assembly assy = System.Reflection.Assembly.GetEntryAssembly();
            if (assy != null)
            {
                //MessageBox.Show("A:" + (String)(assy.Location ?? ""));
                return Open(assy.Location ?? "");
            }
            //no path
            return Open("");
        }

        ///<summary>Get this configuration set from a specific config file</summary>
        public static ConfigData Open(string path)
        {
            if (instance == null)
            {
                if (path.EndsWith(".config", StringComparison.InvariantCultureIgnoreCase))
                    spath = path.Remove(path.Length - 7);
                else
                    spath = path;
                Configuration config = ConfigurationManager.OpenExeConfiguration(spath);
                if (config.Sections["ConfigData"] == null)
                {
                    instance = new ConfigData();
                    config.Sections.Add("ConfigData", instance);
                    config.Save(ConfigurationSaveMode.Modified);
                }
                else
                    instance = (ConfigData)config.Sections["ConfigData"];
            }
            return instance;
        }

        ///<summary>Create a full copy of the current properties</summary>
        public ConfigData Copy()
        {
            ConfigData copy = new ConfigData();
            copy._configs = (XmlDocument)this._configs.Clone();
            return copy;
        }

        ///<summary>Save the current property values to the config file</summary>
        public void Save()
        {
            // The Configuration has to be opened anew each time we want to update the file contents.
            // Otherwise, the update of other custom configuration sections will cause an exception
            // to occur when we try to save our modifications, stating that another app has modified
            // the file since we opened it.
            Configuration config = ConfigurationManager.OpenExeConfiguration(spath);
            ConfigData section = (ConfigData)config.Sections["ConfigData"];
            section.LockItem = true;
            section._configs = (XmlDocument)this._configs.Clone();
            config.Save(ConfigurationSaveMode.Full);
        }

        /// <summary>
        /// return configuration root node
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public XmlNode getConfig(string name)
        {
            if(_configs == null)
            {
                //probably design or missing
                return null;
            }

            //return this[name] as KeyValueMap;
            if (_configs.DocumentElement == null)
            {
                _configs.AppendChild(_configs.CreateElement("ConfigData"));
            }
            
            XmlNode ret = _configs.DocumentElement.SelectSingleNode(name);
            if(ret == null)
            {
                ret = _configs.AppendChild(_configs.CreateElement(name));
            }
            return ret;
        }

       

        #endregion Public Methods

        #region Properties

        /// <summary>
        /// Defualt config data
        /// </summary>
        public static ConfigData Default
        {
            get { return defaultInstance; }
        }

        #endregion Properties

        #region Fields

        XmlDocument _configs;

        private static string spath;
        private static ConfigData instance = null;
        private static readonly ConfigData defaultInstance = new ConfigData();
        #endregion Fields




        /// <summary>
        /// Serialize
        /// </summary>
        /// <param name="parentElement"></param>
        /// <param name="name"></param>
        /// <param name="saveMode"></param>
        /// <returns></returns>
        protected override string SerializeSection(ConfigurationElement parentElement, string name, ConfigurationSaveMode saveMode)
        {
            StringWriter sWriter = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
            XmlTextWriter xWriter = new XmlTextWriter(sWriter);
            xWriter.Formatting = Formatting.Indented;
            xWriter.Indentation = 4;
            xWriter.IndentChar = ' ';

            _configs.WriteTo(xWriter);
            xWriter.Flush();
            return sWriter.ToString();
        }

        
       


        /// <summary>
        /// DeserializeElement
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="serializeCollectionKey"></param>
        protected override void DeserializeElement(XmlReader reader, bool serializeCollectionKey)
        {
            //string x = reader.ReadOuterXml();
            _configs = new XmlDocument();
            _configs.PreserveWhitespace = false;
            _configs.Load(reader);
        }


        
    }
}
