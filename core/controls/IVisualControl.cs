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

        /// <summary>Starts.</summary>
        ///
        /// <author>Laco</author>
        ///
        /// <param name="startingKind">(Optional) The starting kind.</param>
        /// <param name="data">        (Optional) The data.</param>
        ///
        /// <returns>True if it succeeds, false if it fails.</returns>
		bool Start(
			VisualControlStartingKind startingKind = VisualControlStartingKind.StartingNew,
			object data = null
		);

		RibbonControl Ribbon { get; }

        // method will check if document can be closed
        bool checkClosable();
    }
}

