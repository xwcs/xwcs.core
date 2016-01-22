using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xwcs.core.plgs;
using xwcs.core.evt;
using System.Runtime.CompilerServices;

namespace xwcs.core.manager
{
    public class SWidgetManager
    {

        private static SWidgetManager instance;

        //singleton need private ctor
        private SWidgetManager(){
            int i = 0;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static SWidgetManager getInstance()
        {
            if (instance == null)
            {
                instance = new SWidgetManager();
            }
            return instance;
        }


        /****

            MAIN methods
        */

        private List<WidgetDescriptor> _widgets = new List<WidgetDescriptor>();
        
        /*
        public SWidgetManager()
        {
            _proxy = EventProxy.getInstance();
            //_proxy.addEventHandler(EventType.OpenPanelRequestEvent, HandleOpenPanelRequestEvent);
        }
        */

        public void addWidget(WidgetDescriptor wdscr)
        {
            _widgets.Add(wdscr);
        }
    }
}
