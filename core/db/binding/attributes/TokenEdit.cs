using System;
using DevExpress.XtraDataLayout;
using DevExpress.XtraEditors.Repository;

namespace xwcs.core.db.binding.attributes
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class TokenEdit : CustomAttribute
	{
		
		public override void applyRetrievingAttribute(IDataLayoutExtender host, FieldRetrievingEventArgs e)
		{
			e.EditorType = typeof(DevExpress.XtraEditors.TokenEdit);
		}

		public override void applyRetrievedAttribute(IDataLayoutExtender host, FieldRetrievedEventArgs e)
		{
			RepositoryItemTokenEdit rle = e.RepositoryItem as RepositoryItemTokenEdit;
			GetFieldOptionsListEventData qd = new GetFieldOptionsListEventData { List = null, FieldName = e.FieldName };
			host.onGetOptionsList(qd);
			if (qd.List != null)
			{
                foreach (KeyValuePair pair in qd.List)
                {
                    rle.Tokens.Add(new DevExpress.XtraEditors.TokenEditToken(pair.Value, pair.Key));
                }
            }
		}
	}
}
