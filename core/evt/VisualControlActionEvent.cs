namespace xwcs.core.evt
{
	public enum VisualControlActionKind {
		Activated,
		Disposed,
		Changed
	}

    public class VisualControlActionEventData
    {
        public controls.IVisualControl VisualControl { get; private set; }
		public VisualControlActionKind ActionKind { get; protected set; }

        public VisualControlActionEventData(controls.IVisualControl vc, VisualControlActionKind k)
        {
            VisualControl = vc;
			ActionKind = k;
        }
    }

    public class VisualControlActionEvent : Event
    {
        public VisualControlActionEvent(object sender, VisualControlActionEventData requestData) : base(sender, EventType.VisualControlActionEvent, requestData)
        {
        }

		/// <summary>
		/// typed getter
		/// </summary>
		public VisualControlActionEventData RequestData
        {
            set { _data = value; }
            get { return (VisualControlActionEventData)_data; }
        }
    }
}
