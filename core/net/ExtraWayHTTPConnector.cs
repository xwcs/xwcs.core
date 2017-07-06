using System;
using System.Net;
using System.Text;
using xwcs.core.cfg;
using System.Collections.Specialized;


namespace xwcs.core.net
{
	public class ExtraWayHTTPConnector
	{
		private const string MAX_FILE_SIZE = "100000";
		private const string CFG_ADDRESS_PATH = "HttpServer/Adress";
		private const string CFG_DATABASENAME_PATH = "HttpServer/DatabaseName";

		private Config _cfg = new Config("MainAppConfig");
		private static xwcs.core.manager.ILogger _logger = xwcs.core.manager.SLogManager.getInstance().getClassLogger(typeof(ExtraWayHTTPConnector));

		private string _httpAddress;
		private string _databaseName;

		WebClient _client;

		public ExtraWayHTTPConnector(string httpAddress = "localhost", string databaseName = "niter")
		{
			_httpAddress = httpAddress;
			_databaseName = databaseName;

			_client = new WebClient();
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("MAX_FILE_SIZE", MAX_FILE_SIZE);
			_client.QueryString = parameters;
		}

		public string UploadAttachment(string localFileName)
		{
			string addr = "";
			try
			{
				//Prepare address
				addr =	_cfg.getCfgParam(CFG_ADDRESS_PATH, _httpAddress) + 
						"/attach/put?db=" + 
						_cfg.getCfgParam(CFG_DATABASENAME_PATH, _databaseName);

				//Upload file
				_logger.Info("Trying upload file, address : " + addr + ", file name : " + localFileName);
				var responseBytes = _client.UploadFile(addr, null, localFileName);
				_logger.Info("File uploaded, address : " + addr + ", file name : " + localFileName);
				return Encoding.ASCII.GetString(responseBytes);
			}
			catch (Exception ex)
			{
				_logger.Info("Upload file failed! Address : " + addr + ",  Error : " + ex.Message);
			}
			return "";
		}

		public bool DownloadAttachment(string localFileName, string databaseFileName)
		{
			string addr = "";
			try
			{
				//Prepare address
				addr =	_cfg.getCfgParam(CFG_ADDRESS_PATH, _httpAddress) + 
						"/attach/get?db=" + 
						_cfg.getCfgParam(CFG_DATABASENAME_PATH, _databaseName) + 
						"&fileName=" +
						databaseFileName;

				//Download file
				_logger.Info("Trying download file, address : " + addr + ", file name : " + localFileName);
				_client.DownloadFile(addr, localFileName);
				_logger.Info("File downloaded, address : " + addr + ", file name : " + localFileName);
				return true;
			}
			catch (Exception ex)
			{
				_logger.Info("Download attachment failed! Address : " + addr + ",  Error : " + ex.Message);
			}
			return false;
		}
	}
}
