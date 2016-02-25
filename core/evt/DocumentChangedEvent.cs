namespace xwcs.core.evt
{
    public class DocumentChangedRequest
    {

        public xwcs.core.controls.IVisualControl visualControl { get; set; }

        public DocumentChangedRequest(xwcs.core.controls.IVisualControl vc)
        {
            this.visualControl = vc;
        }
    }

    public class DocumentChangedEvent : Event
    {
        public DocumentChangedEvent(object sender, DocumentChangedRequest requestData) : base(sender, EventType.DocumentChangedEvent, requestData)
        {
        }

        public DocumentChangedRequest requestData
        {
            set { _data = value; }
            get { return (DocumentChangedRequest)_data; }
        }
    }
}
