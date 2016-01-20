namespace xwcs.ui.controls
{
    public interface IControlInfo
    {
        string[] Controls { get; }
    }

    public interface IControl
    {
        ControlInfo controlInfo { get;  }
    }
}

