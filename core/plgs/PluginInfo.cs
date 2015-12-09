using System;
using System.Collections.Generic;


namespace xwcs.core.plgs
{
    public enum pluginType { PLGT_undef = 0, PLGT_nonvisual, PLGT_visual };
    public enum pluginAbility { PLGABLT_undef = 0, PLGABLT_submenu, PLGABLT_mainmenu, PLGABLT_toolbar, PLGABLT_usercontrol};

    public class PluginInfo
    {
        string name { get; }
        string version { get; }
        pluginType type { get; }
        Dictionary<pluginAbility, bool> abilities { get; }
        Dictionary<string, Guid> controls { get; }
    }
}
