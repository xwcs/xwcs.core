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
using System.Threading;

namespace xwcs.core.plgs
{
	public static class ExtensionMethods {
		public static string _L<T>(this T input, string key, string def = null) where T : PluginBase
        {
			try {
				return input.RsMan.GetString(key, input.RsManCulture) ?? def;
			}catch(Exception e) {
                SLogManager.getInstance().getClassLogger(typeof(T)).Warn(string.Format(e.Message));
#if DEBUG_TRACE_LOG_ON
                SLogManager.getInstance().getClassLogger(typeof(T)).Debug(string.Format(e.StackTrace));
#endif
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
                    _resourceCulture = Thread.CurrentThread.CurrentUICulture;

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


		public void createPluginInfo(Type pt, string version, PluginKind kind)
        {
            _pluginInfo = new PluginInfo(pt, version, kind);
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

        public Bitmap getBitmapFromFile(string fileName)
        {
            return SPersistenceManager.GetBitmapFromFile(fileName, GetType());
        }

        public Icon getIconFromFile(string fileName)
        {
            return SPersistenceManager.GetIconFromFile(fileName, GetType());
        }

        public void setImageToButtonItem(DevExpress.XtraBars.BarButtonItem buttonItem, string fileName, bool global = false)
        {
            Bitmap bmp = getBitmapFromFile(fileName);
            if (bmp != null) buttonItem.Glyph = bmp;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~PluginBase() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
