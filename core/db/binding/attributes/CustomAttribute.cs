using System;
using DevExpress.XtraDataLayout;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraEditors.Repository;

namespace xwcs.core.db.binding.attributes
{
	public class CustomAttribute : Attribute
	{
		protected volatile int hashCode = 0;
		public virtual void applyRetrievingAttribute(IDataLayoutExtender host, FieldRetrievingEventArgs e) { }
		public virtual void applyRetrievedAttribute(IDataLayoutExtender host, FieldRetrievedEventArgs e) { }
		public virtual RepositoryItem applyGridColumnPopulation(IDataGridSource host, string ColumnName) { return null; }
		public virtual void applyCustomRowCellEdit(IDataGridSource host, CustomRowCellEditEventArgs e) { }
	}
}
