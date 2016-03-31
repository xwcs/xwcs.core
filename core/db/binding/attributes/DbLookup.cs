using System;
using DevExpress.XtraDataLayout;
using DevExpress.XtraEditors.Repository;

namespace xwcs.core.db.binding.attributes
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class DbLookup : CustomAttribute
	{
		
		public string DisplayMember { set; get; }
		public string ValueMember { set; get; }

		public override bool Equals(object obj)
		{
			DbLookup o = obj as DbLookup;
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

		public override void applyRetrievingAttribute(IDataLayoutExtender host, FieldRetrievingEventArgs e)
		{
			e.EditorType = typeof(DevExpress.XtraEditors.LookUpEdit);
		}

		public override void applyRetrievedAttribute(IDataLayoutExtender host, FieldRetrievedEventArgs e)
		{
			RepositoryItemLookUpEdit rle = e.RepositoryItem as RepositoryItemLookUpEdit;
			rle.DisplayMember = DisplayMember;
			rle.ValueMember = ValueMember;
			GetFieldQueryableEventData qd = new GetFieldQueryableEventData { DataSource = null, FieldName = e.FieldName };
			host.onGetQueryable(qd);
			if (qd.DataSource != null)
			{
				rle.DataSource = qd.DataSource;
			}
		}
	}
}
