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
	public class DbRichTextAttribute : CustomAttribute
	{

		// data layout like container
		public override void applyRetrievingAttribute(IDataBindingSource src, FieldRetrievingEventArgs e)
		{
			e.EditorType = typeof(DevExpress.XtraEditors.RichTextEdit);			
		}

		public override void applyRetrievedAttribute(IDataBindingSource src, FieldRetrievedEventArgs e)
		{
			RepositoryItemRichTextEdit rle = e.RepositoryItem as RepositoryItemRichTextEdit;
		}

		public override void applyGridColumnPopulation(IDataBindingSource src, GridColumnPopulated e)
		{
			e.RepositoryItem = new RepositoryItemRichTextEdit();

			RepositoryItemRichTextEdit rle = e.RepositoryItem as RepositoryItemRichTextEdit;
			rle.AutoHeight = true;
			rle.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            rle.DocumentFormat = DevExpress.XtraRichEdit.DocumentFormat.Html;
        }
		public override void applyCustomRowCellEdit(IDataBindingSource src, CustomRowCellEditEventArgs e)
		{
			RepositoryItemRichTextEdit rle = e.RepositoryItem as RepositoryItemRichTextEdit;
		}
		public override void applyCustomEditShown(IDataBindingSource src, ViewEditorShownEventArgs e)
		{
			RepositoryItemRichTextEdit rle = e.RepositoryItem as RepositoryItemRichTextEdit;
		}

		//filter control
		public override void applyCustomEditShownFilterControl(IDataBindingSource src, ShowValueEditorEventArgs e)
		{
			RepositoryItemRichTextEdit rle = new RepositoryItemRichTextEdit();
			e.CustomRepositoryItem = rle;
		}
	}
}