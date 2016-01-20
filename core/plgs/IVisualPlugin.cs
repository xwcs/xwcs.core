using System;
using DevExpress.XtraEditors;


namespace xwcs.core.plgs
{
    public interface IVisualPlugin : IPlugin
    {
        XtraUserControl getControlByGuid(Guid g);
    }
}

