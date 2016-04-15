namespace xwcs.core.controls
{
	public enum VisualControlStartingKind {
		StartingNew,
		StartingPersisted
	}

    public interface IVisualControl
    {
        VisualControlInfo VisualControlInfo { get; }
		string ControlName { get;  }
		void Start(VisualControlStartingKind startingKind = VisualControlStartingKind.StartingNew);
    }
}

