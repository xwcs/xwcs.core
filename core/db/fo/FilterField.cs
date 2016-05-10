using DevExpress.Data.Filtering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Linq;
using System.Runtime.Serialization;

namespace xwcs.core.db.fo
{
	public interface ICriteriaTreeNode {
		CriteriaOperator GetCondition();
		string GetFieldName();
		string GetFullFieldName();
		bool HasCriteria();
	}
	
	[DataContract(IsReference=true)]
	public class FilterField<T> : ICriteriaTreeNode
	{
		#region serialize
		[DataMember(Order = 0)]
		private object _field;
		private CriteriaOperator _condition;
		[DataMember(Order = 1)]
		private string _conditionStr {
			get {
				return !ReferenceEquals(null, _condition) ? _condition.LegacyToString() : "";	
			}
			set {
				_condition = value == "" ? null : CriteriaOperator.Parse(value);
			}
		}
		[DataMember(Order = 2)]
		private bool _hasCriteria = false;
		[DataMember(Order = 3)]
		private string _fieldName;
		[DataMember(Order = 4)]
		private string _fieldFullName;
		#endregion

		#region ICriteriaTreeNode
		public CriteriaOperator GetCondition() { return Condition; }
		public string GetFullFieldName() {
			return _fieldFullName; 
		}
		public string GetFieldName()
		{
			return _fieldName;
		}
		#endregion

		//private need for de-serialize
		private FilterField() : this("", "") { }		
		public FilterField(string pn, string fn) {

			if (typeof(T).IsValueType)
				_field = Activator.CreateInstance(typeof(T));
			else
				_field = null;

			_fieldName = fn;
			_fieldFullName = pn != "" ? pn + "." + fn :  fn;
		}	

		public static implicit operator T(FilterField<T> from)
		{
			return (T)from._field;
		}

		#region Properties

		public T Value {
			get {
				return (T)_field;
			}

			set {
				_field = value;
				_condition = null;
				_hasCriteria = false;
			}
		}

		public CriteriaOperator Condition {
			get {
				if(_hasCriteria) {
					return _condition;
				}else {
					//make one from value
					return _field != null ? new BinaryOperator(GetFullFieldName(), _field, BinaryOperatorType.Equal) : null;
				}
				
			}

			set {
				_condition = value;
				_field = null;
				_hasCriteria = true;
			}
		}

		#endregion

		public bool HasCriteria() {
			return _hasCriteria;
		}
		
		/*
		public bool Cmp(CriteriaOperator rhs) {
			return _condition.Equals(rhs);
		}

		public bool Cmp(FilterField<T> rhs) {
			return Equals((T)_field, rhs.Value) && _condition.Equals(rhs.Condition);
		}
		*/
	}
}
