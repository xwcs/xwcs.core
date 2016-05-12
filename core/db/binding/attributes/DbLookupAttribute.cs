using System;
using DevExpress.XtraDataLayout;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Columns;
using System.Linq;

namespace xwcs.core.db.binding.attributes
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class DbLookupAttribute : CustomAttribute
	{
		RepositoryItemLookUpEdit rle = new RepositoryItemLookUpEdit();
		
		public string DisplayMember { set; get; }
		public string ValueMember { set; get; }


		
		public override bool Equals(object obj)
		{
			DbLookupAttribute o = obj as DbLookupAttribute;
			if (o != null)
			{
				return DisplayMember.Equals(o.DisplayMember) && ValueMember.Equals(o.ValueMember);
			}
			return false;
		}

		public override int GetHashCode()
		{
			int multiplier = 23;
			if (hashCode == 0)
			{
				int code = 133;
				code = multiplier * code + DisplayMember.GetHashCode();
				code = multiplier * code + ValueMember.GetHashCode();
				hashCode = code;
			}
			return hashCode;
		}

		// grid like container
		public override RepositoryItem applyGridColumnPopulation(IDataBindingSource src, string ColumnName) {
			return new RepositoryItemLookUpEdit();
		}
		public override void applyCustomRowCellEdit(IDataBindingSource src, CustomRowCellEditEventArgs e) {
			RepositoryItemLookUpEdit rle = e.RepositoryItem as RepositoryItemLookUpEdit;
			setupRle(src, rle, e.Column.FieldName);
		}

		// data layout like container
		public override void applyRetrievingAttribute(IDataBindingSource src, FieldRetrievingEventArgs e)
		{
			e.EditorType = typeof(DevExpress.XtraEditors.LookUpEdit);
		}
		public override void applyRetrievedAttribute(IDataBindingSource src, FieldRetrievedEventArgs e)
		{
			RepositoryItemLookUpEdit rle = e.RepositoryItem as RepositoryItemLookUpEdit;
			setupRle(src, rle, e.FieldName);
		}

		private void setupRle(IDataBindingSource src, RepositoryItemLookUpEdit rle, string fn) {
			rle.DisplayMember = DisplayMember;
			rle.ValueMember = ValueMember;
			rle.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
			GetFieldQueryableEventData qd = new GetFieldQueryableEventData { DataSource = null, FieldName = fn };
			src.EditorsHost.onGetQueryable(this, qd);
			if (qd.DataSource != null)
			{
				rle.DataSource = qd.DataSource;
			}
		}
	}
}