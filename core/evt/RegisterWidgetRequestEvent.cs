using System;
using DevExpress.XtraBars.Docking;
using DevExpress.XtraEditors;
using xwcs.core.plgs;

namespace xwcs.core.evt
{
    public class RegisterWidgetRequest
    {
        //Private
        private WidgetDescriptor _widgetDescriptor;

        public RegisterWidgetRequest(WidgetDescriptor wdscr)
        {
            _widgetDescriptor = wdscr;
        }

        public WidgetDescriptor WidgetDescriptor
        {
            get { return _widgetDescriptor; }
            set { _widgetDescriptor = value; }
        }
    }

    public class RegisterWidgetRequestEvent : Event
    {
        public RegisterWidgetRequestEvent(object sender, RegisterWidgetRequest requestData) : base(sender, EventType.RegisterWidgetRequestEvent, requestData)
        {
        }

        RegisterWidgetRequest requestData
        {
            get { return (RegisterWidgetRequest)_data; }
            set { _data = value; }
        }
    }
}