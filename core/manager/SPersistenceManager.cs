using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;

namespace xwcs.core.manager
{

    [xwcs.core.cfg.attr.Config("MainAppConfig")]
    public class SPersistenceManager : xwcs.core.cfg.Configurable
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

        public void createWorkSpace(String path)
        {
            // Create the file.
            FileStream fs = File.Create(path + "/meta");
            if (fs != null)
            {
                Byte[] info = new UTF8Encoding(true).GetBytes("This is metafile for workspace");
                fs.Write(info, 0, info.Length);
                fs.Close();

                setWorkSpace(path);
            }            
        }

        public void setWorkSpace(String path)
        {
            //throw exception is file does nort exists
            FileStream fs = File.Open(path + "/meta", FileMode.Open);
            fs.Close();

            XmlNode n = getCfgParamNode("StateData/path");
            n.InnerText = path;
            xwcs.core.cfg.ConfigData.Open().Save();
        }


        public Stream getWriter(String key)
        {
            String path = getCfgParam("StateData/path", "") + "\\" + key + ".xml";
            return new FileStream(path, FileMode.OpenOrCreate);
        }


        public Stream getReader(String key)
        {
            String path = getCfgParam("StateData/path", "") + "\\" + key + ".xml";
            Console.WriteLine("getReader : " + path);
            return new FileStream(path, FileMode.Open);
        }
    }
}
