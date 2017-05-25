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
			//e.EditorType = typeof(DevExpress.XtraEditors.RichTextEdit);
			e.EditorType = typeof(DevExpress.XtraEditors.MemoExEdit);
		}

		public override void applyRetrievedAttribute(IDataBindingSource src, FieldRetrievedEventArgs e)
		{
			//RepositoryItemRichTextEdit rle = e.RepositoryItem as RepositoryItemRichTextEdit;
			
			RepositoryItemMemoExEdit rle = e.RepositoryItem as RepositoryItemMemoExEdit;	
			//rle.LinesCount = 5;
			//rle.AutoHeight = false;
			//rle.WordWrap = true;
		}
	}
}