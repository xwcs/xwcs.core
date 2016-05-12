using System;
using DevExpress.XtraDataLayout;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraEditors.Repository;

namespace xwcs.core.db.binding.attributes
{
	public class CustomAttribute : Attribute
	{
		protected volatile int hashCode = 0;
		public virtual void applyRetrievingAttribute(IDataBindingSource scr, FieldRetrievingEventArgs e) { }
		public virtual void applyRetrievedAttribute(IDataBindingSource src, FieldRetrievedEventArgs e) { }
		public virtual RepositoryItem applyGridColumnPopulation(IDataBindingSource src, string ColumnName) { return null; }
		public virtual void applyCustomRowCellEdit(IDataBindingSource src, CustomRowCellEditEventArgs e) { }
		//do eventual cleaning here
		public virtual void unbind(IDataBindingSource src) { }
	}
}
