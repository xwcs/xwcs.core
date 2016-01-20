using xwcs.core.evt;
using xwcs.core.user;

namespace xwcs.core.plgs
{
    public interface IPluginHost
    {
        EventProxy eventProxy { get; }
        IUser currentUser { get;  }
    }
}
