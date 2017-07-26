using System;
using System.Net;
using System.Text;
using xwcs.core.cfg;
using System.Collections.Specialized;
using System.Windows.Forms;

namespace xwcs.core.net
{
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
				addr =	GetHttpBaseUrl("attach/put");

				//Upload file
				_logger.Debug("Trying upload file, address : " + addr + ", file name : " + localFileName);
				var responseBytes = _client.UploadFile(addr, null, localFileName);
				_logger.Debug("File uploaded, address : " + addr + ", file name : " + localFileName);
				return Encoding.ASCII.GetString(responseBytes);
			}
			catch (Exception ex)
			{
				_logger.Debug("Upload file failed! Address : " + addr + ",  Error : " + ex.Message);
			}
			return "";
		}

		public bool DownloadAttachment(string localFileName, string databaseFileName)
		{
			string addr = "";
			try
			{
				//Prepare address
				addr =	GetHttpBaseUrl("attach/get") + 
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
				_logger.Debug("Download attachment failed! Address : " + addr + ",  Error : " + ex.Message);
			}
			return false;
		}
	}
}
