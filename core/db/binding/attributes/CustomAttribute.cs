﻿using System;
using DevExpress.XtraDataLayout;
using DevExpress.XtraGrid.Views.Grid;

namespace xwcs.core.db.binding.attributes
{
	public class CustomAttribute : Attribute
	{
		protected volatile int hashCode = 0;
		public virtual void applyRetrievingAttribute(IDataLayoutExtender host, FieldRetrievingEventArgs e) { }
		public virtual void applyRetrievedAttribute(IDataLayoutExtender host, FieldRetrievedEventArgs e) { }
		public virtual void applyGridColumnPopulation(IDataGridSource host, string ColumnName) { }
		public virtual void applyCustomRowCellEdit(IDataLayoutExtender host, CustomRowCellEditEventArgs e) { }
	}
}
