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

        private List<xwcs.core.controls.WidgetDescriptor> _widgets = new List<xwcs.core.controls.WidgetDescriptor>();

        public List<xwcs.core.controls.WidgetDescriptor> Widgets
        {
            get { return _widgets; }
        }

        public void addWidget(xwcs.core.controls.WidgetDescriptor wdscr)
        {
            _widgets.Add(wdscr);
        }
    }
}
