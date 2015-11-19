using xwcs.core.evt;

namespace xwcs.core.plgs
{
    public interface IPluginHost
    {
        EventProxy proxy { get; }
    }
}
