using System;
using DevExpress.XtraDataLayout;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Columns;
using System.Linq;
using DevExpress.XtraEditors.Filtering;
using DevExpress.XtraGrid.Views.Base;

namespace xwcs.core.db.binding.attributes
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class DbMemoAttribute : CustomAttribute
	{

		// data layout like container
		public override void applyRetrievingAttribute(IDataBindingSource src, FieldRetrievingEventArgs e)
		{
			e.EditorType = typeof(DevExpress.XtraEditors.MemoEdit);
		}

		public override void applyRetrievedAttribute(IDataBindingSource src, FieldRetrievedEventArgs e)
		{
			RepositoryItemMemoEdit rle = e.RepositoryItem as RepositoryItemMemoEdit;
			rle.WordWrap = true;
		}

		public override void applyGridColumnPopulation(IDataBindingSource src, GridColumnPopulated e)
		{
			e.RepositoryItem = new RepositoryItemMemoEdit();

			RepositoryItemMemoEdit rle = e.RepositoryItem as RepositoryItemMemoEdit;
			rle.WordWrap = true;
			rle.AutoHeight = true;
			rle.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
		}
		public override void applyCustomRowCellEdit(IDataBindingSource src, CustomRowCellEditEventArgs e)
		{
			RepositoryItemMemoEdit rle = e.RepositoryItem as RepositoryItemMemoEdit;
		}
		public override void applyCustomEditShown(IDataBindingSource src, ViewEditorShownEventArgs e)
		{
			RepositoryItemMemoEdit rle = e.RepositoryItem as RepositoryItemMemoEdit;
		}

		//filter control
		public override void applyCustomEditShownFilterControl(IDataBindingSource src, ShowValueEditorEventArgs e)
		{
			RepositoryItemMemoEdit rle = new RepositoryItemMemoEdit();
			e.CustomRepositoryItem = rle;
		}
	}
}