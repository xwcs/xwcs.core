
using System;
using DevExpress.XtraBars.Docking;
using DevExpress.XtraEditors;

namespace xwcs.core.evt
{
    public class OpenPanelRequest
    {
        //Private
        private DockingStyle _position;
        private XtraUserControl _control;
        private Guid _guid;

        //Public getters, setters
        public DockingStyle position
        {
            get { return _position; }
            set { _position = position; }
        }

        public XtraUserControl control
        {
            get { return _control; }
            set { _control = control; }
        }

        public Guid guid
        {
            get { return _guid; }
            set { _guid = guid; }
        }

        //Contructors
        public OpenPanelRequest(DockingStyle position, XtraUserControl control, Guid guid)
        {
            _position = position;
            _control = control;
            _guid = guid;
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
