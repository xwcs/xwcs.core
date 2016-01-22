using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xwcs.core.plgs;
using xwcs.core.evt;

namespace xwcs.core.manager
{
    public class WidgetManager
    {
        private List<WidgetDescriptor> _widgets = new List<WidgetDescriptor>();
        private EventProxy _proxy;

        public WidgetManager(EventProxy p)
        {
            _proxy = p;
            //_proxy.addEventHandler(EventType.OpenPanelRequestEvent, HandleOpenPanelRequestEvent);
        }

        private void addWidget(WidgetDescriptor wdscr)
        {
            _widgets.Add(wdscr);
        }
    }
}
