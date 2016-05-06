namespace xwcs.core.evt
{
	public class DocumentChangedEventData : VisualControlActionEventData {
		public DocumentChangedEventData(controls.IVisualControl vc) : base(vc, VisualControlActionKind.Changed)
		{ 
		}
	}

	public class DocumentChangedEvent : VisualControlActionEvent
    {
        public DocumentChangedEvent(object sender, DocumentChangedEventData requestData) : base(sender, requestData)
        {
			Type = EventType.DocumentChangedEvent; //force type
		}
    }
}
