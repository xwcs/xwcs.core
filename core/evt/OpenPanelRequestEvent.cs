namespace xwcs.core.evt
{

    public class OpenPanelRequest
    {

        public DevExpress.XtraBars.Docking.DockingStyle where { get; set; }
        public DevExpress.XtraEditors.XtraUserControl control {get; set;}
        public string guid { get; set; }

        public OpenPanelRequest(DevExpress.XtraBars.Docking.DockingStyle where, DevExpress.XtraEditors.XtraUserControl control, string guid)
        {
            this.where = where;
            this.control = control;
            this.guid = guid;
        }
    }

    public class OpenPanelRequestEvent : Event
    {
        public OpenPanelRequestEvent(object sender, OpenPanelRequest requestData) : base(sender, EventType.OpenPanelRequestEvent, requestData)
        {
        }

        public OpenPanelRequest requestData
        {
            set { _data = value; }
            get { return (OpenPanelRequest) _data; }
        }
    }
}
