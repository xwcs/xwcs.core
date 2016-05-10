﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;

namespace xwcs.core.db.fo
{
	using DevExpress.Data.Filtering;
	using model;
	using model.attributes;
	using System.Collections.Generic;
	using System.Xml.Serialization;
	using System.Xml;
	using System.Xml.Schema;
	using System.Runtime.Serialization;
	using System.Reflection;
	using evt;
	[CollectionDataContract(IsReference = true)]
	public class FilterObjectList<T> : List<T> where T : ICriteriaTreeNode {}

	[DataContract(IsReference = true)]
	public class FilterObjectsCollection <T> : ICriteriaTreeNode where T : ICriteriaTreeNode
	{
		#region serialize
		[DataMember(Order = 0)]
		private FilterObjectList<T> _data;
		[DataMember(Order = 1)]
		private string _fieldName;
		[DataMember(Order = 2)]
		private string _fieldFullName;

		#endregion

		#region ICriteriaTreeNode
		public CriteriaOperator GetCondition()
		{
			return new ContainsOperator(GetFullFieldName(), CriteriaOperator.Or(this._data.Select(o => o.GetCondition()).AsEnumerable()));
		}
		public string GetFullFieldName()
		{
			return _fieldFullName;
		}
		public string GetFieldName()
		{
			return _fieldName;
		}
		#endregion


		//private need for de-serialize
		private FilterObjectsCollection() : this("", "") {}
		public FilterObjectsCollection(string pn, string fn) : base()
		{
			_data = new FilterObjectList<T>();
			_fieldName = fn;
			_fieldFullName = pn != "" ? pn + "." + fn : fn;
		}

		public ICollection<T> Data {
			get {
				return _data;
			}
			set {
				_data.Clear();
				if(value != null)
					_data.AddRange(value);
			}
		}

		public bool HasCriteria()
		{
			return true;
		}
	} 


	[TypeDescriptionProvider(typeof(HyperTypeDescriptionProvider))]
	[DataContract(IsReference = true)]
	public abstract class FilterObjectbase : INotifyPropertyChanged, ICriteriaTreeNode//, IXmlSerializable
	{
		//public event PropertyChangedEventHandler PropertyChanged;
		private WeakEventSource<PropertyChangedEventArgs> _wes_PropertyChanged = new WeakEventSource<PropertyChangedEventArgs>();
		public event PropertyChangedEventHandler PropertyChanged
		{
			add
			{
				if (_wes_PropertyChanged == null)
				{
					_wes_PropertyChanged = new WeakEventSource<PropertyChangedEventArgs>();
				}
				_wes_PropertyChanged.Subscribe(new EventHandler<PropertyChangedEventArgs>(value));
			}
			remove { _wes_PropertyChanged.Unsubscribe(new EventHandler<PropertyChangedEventArgs>(value)); }
		}

		#region serialize
		[DataMember(Order = 0)]
		private string _fieldName;
		[DataMember(Order = 1)]
		private string _fieldFullName;
		#endregion

		#region ICriteriaTreeNode

		public CriteriaOperator GetCondition() {
			//build condition
			return CriteriaOperator.And(
					this.GetType()
					.GetFields(System.Reflection.BindingFlags.NonPublic | BindingFlags.Instance)
					.Select(field => field.GetValue(this))
					.Cast<ICriteriaTreeNode>()
					.Select(c => c.GetCondition())
					.AsEnumerable()
			);
		}
		public string GetFullFieldName()
		{
			return _fieldFullName;
		}
		public string GetFieldName()
		{
			return _fieldName;
		}
		#endregion

		public FilterObjectbase() : this("", "") { }

		public FilterObjectbase(string pn, string fn)
		{
			if(_wes_PropertyChanged == null) _wes_PropertyChanged = new WeakEventSource<PropertyChangedEventArgs>();
			_fieldName = fn;
			_fieldFullName = pn != "" ? pn + "." + fn : fn;
		}

		protected void SetField<T>(ref FilterField<T> storage, object value, [CallerMemberName] string propertyName = null)
		{
			storage.Value = (T)value;
			//PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			_wes_PropertyChanged.Raise(this, new PropertyChangedEventArgs(propertyName));
		}

		protected void SetFieldCriteria<T>(ref FilterField<T> storage, CriteriaOperator value, [CallerMemberName] string propertyName = null)
		{
			var oldValue = storage.Value;	
			storage.Condition = value;
			//condition set value to null, so if old was something else notify property change
			if(oldValue != null)
				//PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
				_wes_PropertyChanged.Raise(this, new PropertyChangedEventArgs(propertyName)); //value set to null
		}

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			//PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			_wes_PropertyChanged.Raise(this, new PropertyChangedEventArgs(propertyName));
		}

		public ICriteriaTreeNode GetFilterFieldByPath(string path) {
			return GetFilterFieldByPath(this, path);
		}

		private ICriteriaTreeNode GetFilterFieldByPath(ICriteriaTreeNode obj, string path) {
			if (obj == null) { return null; }

			int l = path.IndexOf(".");
			string name = path;
			string suffix = "";
			if (l > 0)
			{
				name = path.Substring(0, l);
				suffix = path.Substring(l + 1);
			}

			try {
				obj = obj.GetType()
				.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
				.Where(field => field.Name == '_' + name)
				.Select(field => field.GetValue(obj))
				.Single() as ICriteriaTreeNode;
			}catch(Exception) {
				return null;
			}			
			
			//not done			
			if(suffix != "") {
				return GetFilterFieldByPath(obj, suffix);
			}

			//done
			return obj;
		}

		public bool HasCriteria()
		{
			return true;
		}
	}
}
