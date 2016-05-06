using System;
using System.Text;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Serialization;

namespace xwcs.core.manager
{

    [xwcs.core.cfg.attr.Config("MainAppConfig")]
    public class SPersistenceManager : cfg.Configurable
    {

        private static SPersistenceManager instance;

        //singleton need private ctor
        private SPersistenceManager()
        {
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
				XmlSerializer serial = new XmlSerializer(typeof(T));
				writer = GetWriter(key);
				serial.Serialize(writer, what);
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
	}
}
