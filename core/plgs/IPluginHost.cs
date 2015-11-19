using xwcs.core.evt;

namespace xwcs.core.plgs
{
    public interface IPluginHost
    {
        EventProxy eventProxy { get; }
    }
}
