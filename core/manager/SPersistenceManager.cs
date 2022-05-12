using System;
using System.Text;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Drawing;

namespace xwcs.core.manager
{
	[xwcs.core.cfg.attr.Config("MainAppConfig")]
    public class SPersistenceManager : cfg.Configurable
    {
		public enum AssetKind{
			Image,
			Layout,
			Print,
            JavaScript,
            Any
		}

        private static SPersistenceManager instance;
        
        //singleton need private ctor
        private SPersistenceManager()
        {
        }

        public bool IsAllowed_LoadLayoutFromXml
        {
            get
            {
                return "Yes".Equals(getCfgParam("DataLayout/AllowLoadFromXml", "No"));
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static SPersistenceManager getInstance()
        {
            if (instance == null)
            {
                instance = new SPersistenceManager();
            }
            return instance;
        }

        public void CreateWorkSpace(string path)
        {
			Directory.CreateDirectory(path);
			// Create the file.
			FileStream fs = File.Create(path + "/meta");
            if (fs != null)
            {
                byte[] info = new UTF8Encoding(true).GetBytes("This is metafile for workspace");
                fs.Write(info, 0, info.Length);
                fs.Close();

                SetWorkSpace(path);
            }            
        }

        public void SetWorkSpace(string path)
        {
            //throw exception is file does nort exists
            FileStream fs = File.Open(path + "/meta", FileMode.Open);
            fs.Close();

            XmlNode n = getCfgParamNode("StateData/path");
            n.InnerText = path;
            cfg.ConfigData.Open().Save();
        }


        public Stream GetWriter(string key)
        {
            string path = NormalizePath(getCfgParam("StateData/path", "") + "\\" + key);
			if (File.Exists(path))
            {
                return new FileStream(path, FileMode.Truncate);
            }
            return new FileStream(path, FileMode.OpenOrCreate);
        }


        public Stream GetReader(string key)
        {
			string path = NormalizePath(getCfgParam("StateData/path", "") + "\\" + key);
			if (File.Exists(path))
			{
				return new FileStream(NormalizePath(getCfgParam("StateData/path", "") + "\\" + key), FileMode.Open);
			}
			return null;
        }

		/// <summary>
		/// this will produce normalized file name and ensure path existence
		/// </summary>
		/// <param name="path">Base path without extension</param>
		/// <param name="ext">File extension</param>
		/// <returns></returns>
		private string NormalizePath(string path, string ext = "xml") {
			string tmp = path.Replace('.', '\\').Replace('/', '\\') + (ext.Length > 0 ? "." + ext : "");
			Directory.CreateDirectory(Path.GetDirectoryName(tmp));
			return tmp;
		}


		public void SaveObject<T>(string key, T what) {
			Stream writer = null;
			try
			{

				//XmlSerializer serial = new XmlSerializer(typeof(T));
				NetDataContractSerializer serial = new NetDataContractSerializer();
				writer = GetWriter(key);
				serial.WriteObject(writer, what);
			}
			catch (Exception e)
			{
				SLogManager.getInstance().getClassLogger(GetType()).Error(e.Message);
			}
			finally
			{
				writer.Close();
			}
		}

		public bool LoadObject<T>(string key, ref T dest)
		{
			Stream reader = null;
			try
			{
				reader = GetReader(key);

				if (reader != null)
				{
					NetDataContractSerializer serial = new NetDataContractSerializer();
					//XmlSerializer serial = new XmlSerializer(typeof(T));
					dest = (T)serial.ReadObject(reader);
					return true;
				}
			}
			catch (Exception e)
			{
				SLogManager.getInstance().getClassLogger(GetType()).Error(e.Message);
			}
			finally
			{
				if (reader != null) reader.Close();
			}

			return false;
		}

        public bool LoadObjectFromXmlNode<T>(XmlNode node, ref T dest)
        {
            XmlNodeReader reader = new XmlNodeReader(node);
            try
            {
                
                if (reader != null)
                {
                    //NetDataContractSerializer serial = new NetDataContractSerializer();
                    XmlSerializer serial = new XmlSerializer(typeof(T));
                    dest = (T)serial.Deserialize(reader);
                    return true;
                }
            }
            catch (Exception e)
            {
                SLogManager.getInstance().getClassLogger(GetType()).Error(e.Message);
            }
            finally
            {
                if (reader != null) reader.Close();
            }

            return false;
        }

        private static string AssetsPath(AssetKind k) {
			switch(k) {
				case AssetKind.Image: return Path.DirectorySeparatorChar + "img";
				case AssetKind.Layout: return Path.DirectorySeparatorChar + "layout";
				case AssetKind.Print: return Path.DirectorySeparatorChar + "print";
                case AssetKind.JavaScript: return Path.DirectorySeparatorChar + "js";
                case AssetKind.Any: 
                default: return "";
            }
		}

		//file system support
		public static string GetDefaultAssetsPath(AssetKind kind, Type t = null) {
            return GetDefaultAssetsPath() + AssetsPath(kind) + (t != null ? (Path.DirectorySeparatorChar + t.Namespace) : "");
		}
        public static string GetDefaultAssetsPath()
        {
            return AppDomain.CurrentDomain.BaseDirectory + "assets";
        }


        private static string _pattern = @"(?:{(\w[^}^\s]+)})";
        
        /// <summary>
        /// This will handle eventual place holders in path
        /// place holders
        /// {temp}          - %temp%
        /// {user}          - %appdata%
        /// {run}           - path of executable
        /// {assets}        - assets root
        /// {assets_img}    - assets img
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string TemplatizePath(string path)
        {
            return Path.GetFullPath(Regex.Replace(path, _pattern, new MatchEvaluator(Includer))).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        private static string Includer(Match match)
        {
            string fName = match.Groups[1].Value;

            switch (fName)
            {
                case "temp": return Path.GetTempPath();
                case "user": return "%APPDATA%";
                case "run": return AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
                case "assets": return GetDefaultAssetsPath();
                default: return fName;
            }            
        }


        public static Bitmap GetBitmapFromFile(string fileName, Type host = null)
        {
            Bitmap bitmap = null;
            try
            {
                bitmap = (Bitmap)Image.FromFile(GetDefaultAssetsPath(AssetKind.Image, host) + Path.DirectorySeparatorChar + fileName, true);
            }
            catch (Exception e)
            {
                // ERROR C:\ProgramData\EgafBOiter\assets\img\plugin.pubblicazione\iter_h16_bordo.png - GetBitmapFromFile(0) #293
                SLogManager.getInstance().Info(e.Message);
                return null;
            }
            return bitmap;
        }

        public static Icon GetIconFromFile(string fileName, Type host = null)
        {
            Icon bitmap = null;
            try
            {
                bitmap = Icon.ExtractAssociatedIcon(GetDefaultAssetsPath(AssetKind.Image, host) + Path.DirectorySeparatorChar + fileName);
            }
            catch (Exception e)
            {
                SLogManager.getInstance().Error(e.Message);
                return null;
            }
            return bitmap;
        }
    }
}

