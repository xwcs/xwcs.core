using System;
using DevExpress.XtraDataLayout;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Views.Grid;

namespace xwcs.core.db.binding.attributes
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class TokenEditAttribute : CustomAttribute
	{
		
		public override void applyRetrievingAttribute(IDataBindingSource src, FieldRetrievingEventArgs e)
		{
			e.EditorType = typeof(DevExpress.XtraEditors.TokenEdit);
		}

		public override void applyRetrievedAttribute(IDataBindingSource src, FieldRetrievedEventArgs e)
		{
			RepositoryItemTokenEdit rle = e.RepositoryItem as RepositoryItemTokenEdit;
			setupRle(src, rle, e.FieldName);
		}

		// grid like container
		public override void applyGridColumnPopulation(IDataBindingSource src, GridColumnPopulated e)
		{
			e.RepositoryItem = new RepositoryItemTokenEdit();
		}
		public override void applyCustomRowCellEdit(IDataBindingSource src, CustomRowCellEditEventArgs e)
		{
		}
		public override void applyCustomEditShown(IDataBindingSource src, ViewEditorShownEventArgs e)
		{
			RepositoryItemTokenEdit rle = e.RepositoryItem as RepositoryItemTokenEdit;
			setupRle(src, rle, e.FieldName);
		}

		private void setupRle(IDataBindingSource src, RepositoryItemTokenEdit rle, string fn)
		{
			GetFieldOptionsListEventData qd = new GetFieldOptionsListEventData { Data = null, FieldName = fn, DataBindingSource = src};
			src.EditorsHost.onGetOptionsList(this, qd);
			if (qd.Data != null)
			{
				foreach (KeyValuePair pair in qd.Data)
				{
					rle.Tokens.Add(new DevExpress.XtraEditors.TokenEditToken(pair.Value, pair.Key));
				}
			}
		}
	}
}
