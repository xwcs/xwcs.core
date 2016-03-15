using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xwcs.core.evt;
using System.Drawing;
using xwcs.core.manager;

namespace xwcs.core.plgs
{
    public abstract class PluginBase : IPlugin
    {

        private PluginInfo _pluginInfo;
        private xwcs.core.evt.SEventProxy _eventProxy;


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
            _eventProxy = xwcs.core.evt.SEventProxy.getInstance();
        }

        abstract public void init();



        private string getAssestsDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory + "/plugins/" + _pluginInfo.Name;
        }

        private Bitmap getBitmapFromFile(string fileName)
        {
            Bitmap bitmap = null;
            try
            {
                bitmap = (Bitmap)Image.FromFile(getAssestsDirectory() + "/" + fileName, true);
            }
            catch(Exception e)
            {
                SLogManager.getInstance().Error(e.Message);
                return null;
            }
            return bitmap;
        }

        public void setImageToButtonItem(DevExpress.XtraBars.BarButtonItem buttonItem, string fileName)
        {
            Bitmap bmp = getBitmapFromFile("images/" + fileName);
            if (bmp != null) buttonItem.Glyph = bmp;
        }
    }
}
