using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xwcs.core.evt;

namespace xwcs.core.plgs
{
    public abstract class VisualPluginBase : PluginBase, IVisualPlugin
    {
        //this should be overriden
        abstract public DevExpress.XtraEditors.XtraUserControl getControlByGuid(Guid guid);

        //should be overriden
        abstract public void afterInit();

        public override void init()
        {
            setup();

            if(Info.Widgets != null)
            {
                xwcs.core.manager.SWidgetManager man = xwcs.core.manager.SWidgetManager.getInstance();
                foreach (WidgetDescriptor w in Info.Widgets.Values)
                {
                    man.addWidget(w);
                }
            }           

            afterInit();            
        }
    }
}
