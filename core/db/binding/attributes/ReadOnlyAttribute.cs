using System;
using DevExpress.XtraDataLayout;
using DevExpress.XtraEditors;

namespace xwcs.core.db.binding.attributes
{
	[AttributeUsage(AttributeTargets.Property)]
	public class ReadOnlyAttribute : CustomAttribute
	{
        // data layout like container
        public override void applyRetrievingAttribute(IDataBindingSource src, FieldRetrievingEventArgs e)
        {
            //e.EditorType = typeof(DevExpress.XtraEditors.TextEdit);
        }
        public override void applyRetrievedAttribute(IDataBindingSource src, FieldRetrievedEventArgs e)
		{
			src.EditorsHost.FormSupport.ControlsMeta[(e.Control as BaseEdit)] = new ui.db.ControlMeta() { ReadOnly = true };

		}

        public override void applyGridColumnPopulation(IDataBindingSource src, GridColumnPopulated e) {
            if (!ReferenceEquals(e.Column, null))
            {
				e.Column.ReadOnly = true;
            }           
        }
    }	
}
