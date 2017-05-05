using System;
using DevExpress.XtraDataLayout;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Columns;
using System.Linq;
using DevExpress.XtraEditors.Filtering;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraRichEdit;

namespace xwcs.core.db.binding.attributes
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class DbRichTextAttribute : CustomAttribute
	{
		// data layout like container
		public override void applyRetrievingAttribute(IDataBindingSource src, FieldRetrievingEventArgs e)
		{
			//e.EditorType = typeof(DevExpress.XtraEditors;
		}
		public override void applyRetrievedAttribute(IDataBindingSource src, FieldRetrievedEventArgs e)
		{
		}

		// grid like container
		public override void applyGridColumnPopulation(IDataBindingSource src, GridColumnPopulated e) {
			//e.RepositoryItem = new Reposi RepositoryItemMemoEdit();
		}
		public override void applyCustomRowCellEdit(IDataBindingSource src, CustomRowCellEditEventArgs e) {
			RepositoryItemMemoEdit rle = e.RepositoryItem as RepositoryItemMemoEdit;
			//rle.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
		}
		public override void applyCustomEditShown(IDataBindingSource src, ViewEditorShownEventArgs e) {
			RepositoryItemMemoEdit rle = e.RepositoryItem as RepositoryItemMemoEdit;
			setupRle(src, rle, e.FieldName);
		}

		//filter control
		public override void applyCustomEditShownFilterControl(IDataBindingSource src, ShowValueEditorEventArgs e) {
			RepositoryItemMemoEdit rle = new RepositoryItemMemoEdit();
			e.CustomRepositoryItem = rle;
			//rle.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
			setupRle(src, rle, e.CurrentNode.FirstOperand.PropertyName);
		}

		private void setupRle(IDataBindingSource src, RepositoryItemMemoEdit rle, string fn) 
		{
			int a;
			a = 1;
			/*
			GetFieldOptionsListEventData qd = new GetFieldOptionsListEventData { Data = null, FieldName = fn, DataBindingSource = src };
			src.EditorsHost.onGetOptionsList(this, qd);
			if (qd.Data != null)
			{

				ComboBoxItemCollection coll = rle.Items;
				coll.BeginUpdate();
				try
				{
					qd.Data.Cast<object>().ToList().ForEach(o => coll.Add(o));
				}
				finally
				{
					coll.EndUpdate();
				}
			}
			*/
		}
	}
}