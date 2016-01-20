using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace xwcs.ui.controls
{
    public partial class VisualControl : DevExpress.XtraEditors.XtraUserControl, xwcs.ui.controls.IControl
    {
        protected xwcs.ui.controls.ControlInfo _controlInfo;

        public ControlInfo controlInfo
        {
            get { return _controlInfo; }
        }
    }
}
