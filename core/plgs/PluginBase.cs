using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xwcs.core.evt;
using System.Drawing;
using xwcs.core.manager;
using System.Resources;
using System.Reflection;

namespace xwcs.core.plgs
{
	public static class ExtensionMethods {
		public static string _L<T>(this T input, string key, string def = null) where T : PluginBase
		{
			try {
				return input.RsMan.GetString(key, input.RsManCulture);
			}catch(Exception e) {
				Console.WriteLine(e.Message);
				Console.Write(e.StackTrace);
				Console.WriteLine("");
				return def ?? key;
			}			
		}
	}

	public abstract class PluginBase : IPlugin
    {

        private PluginInfo _pluginInfo;
        private xwcs.core.evt.SEventProxy _eventProxy;
		
		//strings
		private ResourceManager _resourceManager;
		private System.Globalization.CultureInfo _resourceCulture;

		public ResourceManager RsMan
		{
			get
			{
				if (ReferenceEquals(_resourceManager, null))
				{
					Type thisT = GetType();
					ResourceManager temp = new ResourceManager(thisT.Namespace + ".Properties.Resources", thisT.Assembly);
					_resourceManager = temp;
				}
				return _resourceManager;
			}
		}

		public System.Globalization.CultureInfo RsManCulture
		{
			get
			{
				return _resourceCulture;
			}
			set
			{
				_resourceCulture = value;
			}
		}


		public void createPluginInfo(string name, string version, pluginType type)
        {
            _pluginInfo = new PluginInfo(name, version, type);
        }

        public PluginInfo Info
        {
            get { return _pluginInfo; }
        }

        public SEventProxy EventProxy
        {
            get
            {
                return _eventProxy;
            }
        }

		

		protected void setup()
        {
            //get proxy, so we are sure it exists
            _eventProxy = SEventProxy.getInstance();		
		}

        abstract public void init();



        private string getAssestsDirectory(bool global = false)
        {
            return AppDomain.CurrentDomain.BaseDirectory + "assets" + (!global ? "\\" + _pluginInfo.Namespace : "\\");
        }

        private Bitmap getBitmapFromFile(string fileName, bool global = false)
        {
            Bitmap bitmap = null;
            try
            {
                bitmap = (Bitmap)Image.FromFile(getAssestsDirectory() + "\\" + fileName, true);
            }
            catch(Exception e)
            {
                SLogManager.getInstance().Error(e.Message);
                return null;
            }
            return bitmap;
        }

        public void setImageToButtonItem(DevExpress.XtraBars.BarButtonItem buttonItem, string fileName, bool global = false)
        {
            Bitmap bmp = getBitmapFromFile("img\\" + fileName);
            if (bmp != null) buttonItem.Glyph = bmp;
        }
    }
}
