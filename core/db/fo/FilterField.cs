using DevExpress.Data.Filtering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xwcs.core.db.fo
{
	public class FilterField<T> 
	{
		private object _field;
		private CriteriaOperator _condition;


		public FilterField() {
			if (typeof(T).IsValueType)
				_field = Activator.CreateInstance(typeof(T));
			else
				_field = null;
		}

		public FilterField(T f) {
			_field = f;
		}

		public static implicit operator T(FilterField<T> from)
		{
			return (T)from._field;
		}

		public static implicit operator FilterField<T>(T from)
		{
			return new FilterField<T>(from);
		}

		public T Value {
			get {
				return (T)_field;
			}

			set {
				_field = value;
			}
		}

		public CriteriaOperator Condition {
			get {
				return _condition;
			}

			set {
				_condition = value;
			}
		}

		public bool Cmp(object rhs) {
			return Equals(_field, rhs);
		}

		public bool Cmp(CriteriaOperator rhs) {
			return _condition.Equals(rhs);
		}

		public bool Cmp(FilterField<T> rhs) {
			return Equals((T)_field, rhs.Value) && _condition.Equals(rhs.Condition);
		}
	}
}
