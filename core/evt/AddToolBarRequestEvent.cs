namespace xwcs.core.evt
{

    public class AddToolBarRequest
    {

        public DevExpress.XtraBars.BarButtonItem button {get; set;}
       
        public AddToolBarRequest(DevExpress.XtraBars.BarButtonItem button)
        {
            this.button = button;
        }
    }

    public class AddToolBarRequestEvent : Event
    {
        public AddToolBarRequestEvent(object sender, AddToolBarRequest requestData) : base(sender, EventType.AddToolBarRequestEvent, requestData)
        {
        }

        public AddToolBarRequest requestData
        {
            set { _data = value; }
            get { return (AddToolBarRequest) _data; }
        }
    }
}
