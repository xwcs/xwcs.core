using System;
using System.Net;
using System.Text;
using xwcs.core.cfg;
using System.Collections.Specialized;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Collections;

namespace xwcs.core.net
{

    public class StoredProcResult : IEnumerable
    {
        public StoredProcResult() : this("<?xml version=\"1.0\" encoding=\"UTF-8\"?><rsp ack=\"1\" e=\"1\"><dtl dtype=\"result\" dval=\"Empty!\"/></rsp>") { }
        public StoredProcResult(string rsp) 
        {
            /*
        * <?xml version="1.0" encoding="UTF-8"?>
           <rsp ack="1" e="0">
           <dtl dtype="result" dval="Done!"/></rsp>
        */

            XDocument xdoc = XDocument.Parse(rsp);
            ack = xdoc.Root.Attribute("ack").Value.ToString() == "1";
            e = xdoc.Root.Attribute("e").Value.ToString();

            foreach (var n in xdoc.Descendants("dtl"))
            {
                _dtls[n.Attribute("dtype").Value.ToString()] = n.Attribute("dval").Value.ToString();
            }
        }

        private Dictionary<string, string> _dtls = new Dictionary<string, string>();
        public bool ack { get; private set; }
        public string e { get; private set; }
        public string this[string key]
        {
            get
            {
                string ret = "";
                _dtls.TryGetValue(key, out ret);
                return ret;
            }
            private set
            {
                _dtls[key] = value;
            }
        }     

        public IEnumerator GetEnumerator()
        {
            return _dtls.GetEnumerator();
        }
    }

    public class ExtraWayHTTPConnector
	{
		private const string CFG_MAXFILE_PATH = "ExtraWayHTTPConnector/MaxFileSize";
		private const string CFG_BASEURL_PATH = "ExtraWayHTTPConnector/BaseUrl";
        private const string CFG_DATABASENAME_PATH = "ExtraWayHTTPConnector/Db";

		private static Config _cfg = new Config("MainAppConfig");
		private static xwcs.core.manager.ILogger _logger = xwcs.core.manager.SLogManager.getInstance().getClassLogger(typeof(ExtraWayHTTPConnector));

		private string _httpAddress;
		private string _databaseName;

       

        public static string GetHttpBaseUrl(string resource, string url = "", string db ="")
        {
            return string.Format("{0}{1}?db={2}", 
                    url.Length > 0 ? url : _cfg.getCfgParam(CFG_BASEURL_PATH, ""),
                    resource.Length > 0 ? "/" + resource : "", 
                    db.Length > 0 ? db : _cfg.getCfgParam(CFG_DATABASENAME_PATH, ""));
        }

        public static string PostHttpBaseUrl(string resource, string url = "")
        {
            return string.Format("{0}{1}",
                    url.Length > 0 ? url : _cfg.getCfgParam(CFG_BASEURL_PATH, ""),
                    resource.Length > 0 ? "/" + resource : "");
        }

        WebClient _client;

		public ExtraWayHTTPConnector(string httpAddress = "", string databaseName = "")
		{
			_httpAddress = httpAddress.Length > 0 ? httpAddress : _cfg.getCfgParam(CFG_BASEURL_PATH, "");
			_databaseName = databaseName.Length > 0 ? databaseName : _cfg.getCfgParam(CFG_DATABASENAME_PATH, "");

			_client = new WebClient();
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("MAX_FILE_SIZE", _cfg.getCfgParam(CFG_MAXFILE_PATH, "10000"));
			_client.QueryString = parameters;
		}

		public string UploadAttachment(string localFileName)
		{
			string addr = "";
			try
			{
				//Prepare address
				addr =	GetHttpBaseUrl("attach/put", _httpAddress, _databaseName);

				//Upload file
				_logger.Debug("Trying upload file, address : " + addr + ", file name : " + localFileName);
				var responseBytes = _client.UploadFile(addr, null, localFileName);
				_logger.Debug("File uploaded, address : " + addr + ", file name : " + localFileName);
				return Encoding.ASCII.GetString(responseBytes);
			}
			catch (Exception ex)
			{
				_logger.Debug("Upload file failed! Address : " + addr + ",  Error : " + ex.ToString());
			}
			return "";
		}

		public bool DownloadAttachment(string localFileName, string databaseFileName)
		{
			string addr = "";
			try
			{
				//Prepare address
				addr =	GetHttpBaseUrl("attach/get", _httpAddress, _databaseName) + 
						"&fileName=" +
						databaseFileName;

                Application.UseWaitCursor = true;
                Application.DoEvents();

                //Download file
                _logger.Debug("Trying download file, address : " + addr + ", file name : " + localFileName);
                // ensure dir exists
                string path = System.IO.Path.GetDirectoryName(localFileName);
                System.IO.Directory.CreateDirectory(path);
                _client.DownloadFile(addr, localFileName);
				_logger.Debug("File downloaded, address : " + addr + ", file name : " + localFileName);

                Application.UseWaitCursor = false;
                Application.DoEvents();

                return true;
			}
			catch (Exception ex)
			{
				Application.UseWaitCursor = false;
				Application.DoEvents();
				_logger.Debug("Download attachment failed! Address : " + addr + ",  Error : " + ex.ToString());
			}
			return false;
		}

        

        public StoredProcResult CallStoredProc(string spName, NameValueCollection reqparm)
        {
            string addr = PostHttpBaseUrl("stored");
			if (ReferenceEquals(reqparm, null)) reqparm = new NameValueCollection();

            reqparm.Add("db", _databaseName);
            reqparm.Add("stored", spName);
            byte[] responsebytes = _client.UploadValues(addr, "POST", reqparm);

            return new StoredProcResult(Encoding.UTF8.GetString(responsebytes));            
        }

		public string LoadData(string URL)
		{
			try
			{
				Application.UseWaitCursor = true;
				Application.DoEvents();

				//Download file : TODO
				//_logger.Debug("Trying download file, address : " + addr + ", file name : " + localFileName);
				// ensure dir exists

				byte[] data =_client.DownloadData(URL);

				//_logger.Debug("File downloaded, address : " + addr + ", file name : " + localFileName);

				Application.UseWaitCursor = false;
				Application.DoEvents();

				return Encoding.ASCII.GetString(data);
			}
			catch (Exception)
			{
				Application.UseWaitCursor = false;
				Application.DoEvents();
				//_logger.Debug("Download attachment failed! Address : " + addr + ",  Error : " + ex.ToString());
			}
			return "";
		}


	}
}
