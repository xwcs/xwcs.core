using System;
using DevExpress.XtraDataLayout;
using DevExpress.XtraEditors.Repository;
using xwcs.core.db.binding.attributes;
using xwcs.core.db.binding;

namespace xwcs.core.db.fo.attributes
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class ExtendedConditionAttribute : CustomAttribute
	{
		
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
