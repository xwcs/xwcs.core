using System;
using DevExpress.XtraDataLayout;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraEditors.Filtering;

namespace xwcs.core.db.binding.attributes
{
	public class CustomAttribute : Attribute
	{
		protected volatile int hashCode = 0;
		//layout like
		public virtual void applyRetrievingAttribute(IDataBindingSource scr, FieldRetrievingEventArgs e) { }
		public virtual void applyRetrievedAttribute(IDataBindingSource src, FieldRetrievedEventArgs e) { }
		
		//grid like
		public virtual void applyGridColumnPopulation(IDataBindingSource src, GridColumnPopulated e) { }
		public virtual void applyCustomRowCellEdit(IDataBindingSource src, CustomRowCellEditEventArgs e) { }
		public virtual void applyCustomEditShown(IDataBindingSource src, ViewEditorShownEventArgs e) { }

		//filter control like 
		public virtual void applyCustomEditShownFilterControl(IDataBindingSource src, ShowValueEditorEventArgs e) { }
		

		//do eventual cleaning here
		public virtual void unbind(IDataBindingSource src) { }
	}
}
