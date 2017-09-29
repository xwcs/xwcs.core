
using System;
using DevExpress.XtraBars.Docking;
using DevExpress.XtraEditors;
using xwcs.core.controls;

namespace xwcs.core.evt
{
	//This is very simply event, no data needed only sender (VisualControl)!
	public class ClosePanelRequest
    {      
	}

    public class ClosePanelRequestEvent : Event
    {
        public ClosePanelRequestEvent(object sender) : base(sender, EventType.ClosePanelRequestEvent, null)
        {
        }
    }
}
