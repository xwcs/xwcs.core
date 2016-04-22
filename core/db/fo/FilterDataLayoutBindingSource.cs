using DevExpress.XtraDataLayout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xwcs.core.db.binding;
using xwcs.core.db.model;

namespace xwcs.core.db.fo
{
	public class FilterFieldEventData
	{
		private FieldRetrievedEventArgs _frea = new FieldRetrievedEventArgs();

		public object Field { get; set; }
		public FieldRetrievedEventArgs FREA {
			get { return _frea; }
			set {
				ReflectionHelper.CopyObject(value, _frea);
			}
		}
	}

	public interface IFilterDataLayoutExtender : IDataLayoutExtender
	{
		void onFilterFieldEvent(FilterFieldEventData ffe);
	}

	public class FilterDataLayoutBindingSource : DataLayoutBindingSource, IFilterDataLayoutExtender
	{
		public EventHandler<FilterFieldEventData> FilterFieldEvent;

		public void onFilterFieldEvent(FilterFieldEventData ffe)
		{
			FilterFieldEvent?.Invoke(this, ffe);
		}
	}
}
