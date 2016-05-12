using DevExpress.XtraGrid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xwcs.core.db.binding
{
	public class GetFieldQueryableEventData
	{
		public object DataSource { get; set; }
		public string FieldName { get; set; }
	}

	public class KeyValuePair
	{
		public object Key;
		public string Value;
	}

	public class GetFieldOptionsListEventData
	{
		public List<KeyValuePair> List { get; set; }
		public string FieldName { get; set; }
	}

	public interface IDataBindingSource
	{
		object Current { get; }
		IEditorsHost EditorsHost { get;  }
	}	

	public interface IEditorsHostProvider 
	{
		IEditorsHost EditorsHost { get; set; }
	}

	public interface IEditorsHost
	{
		void onGetOptionsList(object sender, GetFieldOptionsListEventData qd);
		void onGetQueryable(object sender, GetFieldQueryableEventData qd);
	}
}
