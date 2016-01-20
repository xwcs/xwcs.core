using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Xml.Serialization;
using xwcs.core.manager;
using xwcs.core.plgs;
using DevExpress.XtraEditors;
using DevExpress.XtraBars.Docking2010.Views;

namespace xwcs.ui.controls
{

    [XmlRootAttribute("Documents", Namespace = "http://www.cpandl.com", IsNullable = false)]
    public class DocumentManagerState : ManagerStateBase
    {
        [XmlArrayAttribute("Items")]
        public xwcs.ui.controls.ControlInfo[] Documents;
    }

    [xwcs.core.cfg.attr.Config("MainAppConfig")]
    public partial class DocumentManagerSupport : ManagerWithStateBase
    {
        private DevExpress.XtraBars.Docking2010.DocumentManager _manager;
        private PluginsLoader _loader;

        protected DocumentManagerState state { get { return (DocumentManagerState)_managerState; } }

        public DocumentManagerSupport(DevExpress.XtraBars.Docking2010.DocumentManager manager, PluginsLoader loader)
        {
            _manager = manager;
            _loader = loader;
            _managerState = new DocumentManagerState();
        }

        protected override void beforeSave()
        {
            DevExpress.XtraBars.Docking2010.Views.BaseDocumentCollection col = _manager.View.Documents;

            state.Documents = new xwcs.ui.controls.ControlInfo[col.Count];
            int i = 0;
            foreach (DevExpress.XtraBars.Docking2010.Views.BaseDocument document in col)
            {
                VisualControl vc = (VisualControl)document.Control;
                if (vc != null)
                {
                    state.Documents[i++] = vc.controlInfo;
                }
            }
        }

        protected override void afterLoad()
        {
            foreach(ControlInfo ci in state.Documents)
            {
                IVisualPlugin plugin = (IVisualPlugin)_loader.getPluginByGuid(ci.GUID);
                if (plugin != null)
                {
                    VisualControl pluginControl = (VisualControl)plugin.getControlByGuid(ci.GUID);
                    if (pluginControl != null)
                    {
                        _manager.BeginUpdate();
                        BaseDocument document = _manager.View.AddDocument(pluginControl);
                        document.Caption = pluginControl.controlInfo.Name;
                        document.ControlName = pluginControl.controlInfo.Name;
                        _manager.EndUpdate();

                    }
                }
            }
        }
    }
}
