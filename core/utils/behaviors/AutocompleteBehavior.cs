using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Utils.Behaviors;
using DevExpress.Utils.Behaviors.Common;
using System.Drawing;
using System.ComponentModel;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Behaviors;
using xwcs.core.db.binding;

namespace xwcs.core.utils.behaviors
{
    [DisplayName("File Ext Behavior")]
    public sealed class AutocompleteBehavior : PathCompletionBehaviorBase<IFilePathBehaviorSource, FilePathBehaviorProperties>
    {
        private IEditorsHost _host=null;
        public AutocompleteBehavior(Type filePathBehaviorSourceType, IEditorsHost host): base(filePathBehaviorSourceType)
        {
            throw new NotImplementedException();
        } // , iconSize , defaultImage , invalidPathImage , mode , filter ) {}

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static AutocompleteBehavior Create(Type filePathBehaviorSourceType, IEditorsHost host)
        {
            return (AutocompleteBehavior)Behavior.Create(typeof(AutocompleteBehavior), filePathBehaviorSourceType, new object[1]
                {
                                (object) host
                });
        }

        protected override sealed Behavior Clone()
        {
            return (Behavior)new AutocompleteBehavior(this.BehaviorSourceType, _host);
        }

        protected override sealed BehaviorProperties CreateProperties()
        {
            return (BehaviorProperties)new FilePathBehaviorProperties();
        }

        protected override bool CanCompletePath()
        {
            return base.CanCompletePath();
        }

        protected override bool CompletePath(CompletionDirection direction)
        {
            return base.CompletePath(direction);
        }

        protected override void OnPathChanged()
        {
            TextEdit selfedit = (this.Source as TextEdit);

            //base.OnPathChanged();
            QueueUpdate((s) => {
                selfedit.Text = selfedit.Text.ToUpper();
            });

            
        }

        protected override void OnPropertiesChanged()
        {
            base.OnPropertiesChanged();
        }

    }

}
