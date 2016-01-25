
using System;
using DevExpress.XtraBars.Docking;
using DevExpress.XtraEditors;
using xwcs.core.controls;

namespace xwcs.core.evt
{
    public class OpenPanelRequest
    {
        //Private
        xwcs.core.controls.VisualControlInfo _vci;

        public VisualControlInfo Vci
        {
            get
            {
                return _vci;
            }

            set
            {
                _vci = value;
            }
        }
    }

    public class OpenPanelRequestEvent : Event
    {
        public OpenPanelRequestEvent(object sender, OpenPanelRequest requestData) : base(sender, EventType.OpenPanelRequestEvent, requestData)
        {
        }

        public OpenPanelRequest requestData
        {
            get { return (OpenPanelRequest)_data; }
            set { _data = value; }
        }
    }
}
