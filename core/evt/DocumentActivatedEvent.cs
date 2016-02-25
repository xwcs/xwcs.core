namespace xwcs.core.evt
{
    public class DocumentActivatedRequest
    {
        public xwcs.core.controls.IVisualControl visualControl { get; set; }

        public DocumentActivatedRequest(xwcs.core.controls.IVisualControl vc)
        {
            this.visualControl = vc;
        }
    }

    public class DocumentActivatedEvent : Event
    {
        public DocumentActivatedEvent(object sender, DocumentActivatedRequest requestData) : base(sender, EventType.DocumentActivatedEvent, requestData)
        {
        }

        public DocumentActivatedRequest requestData
        {
            set { _data = value; }
            get { return (DocumentActivatedRequest)_data; }
        }
    }
}
