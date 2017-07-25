
using System;
using DevExpress.XtraBars.Docking;
using DevExpress.XtraEditors;
using xwcs.core.controls;

namespace xwcs.core.evt
{
    public class TitleData
    {
        public int nRecord { get; set; }
    }

    public class SetTitleEvent : Event
    {
        public SetTitleEvent(object sender, TitleData data) : base(sender, EventType.SetTitleEvent, data)
        {
        }

        public int nRecord
        {
            get { return ((TitleData)_data).nRecord; }
            set { ((TitleData)_data).nRecord = value; }
        }
    }
}
