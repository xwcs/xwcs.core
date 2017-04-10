using DevExpress.XtraBars.Ribbon;

namespace xwcs.core.controls
{
	public enum VisualControlStartingKind {
		ActivateOpened,
		StartingNew,
		StartingPersisted
	}

    public interface IVisualControl
    {
        VisualControlInfo VisualControlInfo { get; }
		string ControlName { get;  }
		void Start(
			VisualControlStartingKind startingKind = VisualControlStartingKind.StartingNew,
			object data = null
		);

		RibbonControl Ribbon { get; }
    }
}

