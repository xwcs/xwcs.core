
using System;
using DevExpress.XtraBars.Docking;
using DevExpress.XtraEditors;
using xwcs.core.controls;

namespace xwcs.core.evt
{
    public class OutputMessage
    {
        public string Message { get; set; }
    }

    public class OutputMessageEvent : Event
    {
        public OutputMessageEvent(object sender, OutputMessage msg) : base(sender, EventType.OutputMessageEvent, msg)
        {
        }

        public string Message
        {
            get { return ((OutputMessage)_data).Message; }
            set { ((OutputMessage)_data).Message = value; }
        }
    }
}
