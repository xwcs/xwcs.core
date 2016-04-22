using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace xwcs.core.db.fo
{
	using DevExpress.Data.Filtering;
	using model;
	using model.attributes;
	using System.Collections.Generic;

	[TypeDescriptionProvider(typeof(HyperTypeDescriptionProvider))]
	public abstract class FilterObjectbase : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;	
		
		protected bool SetField<T>(ref FilterField<T> storage, object value, [CallerMemberName] string propertyName = null)
		{
			if (storage.Cmp(value)) return false;

			//handle different settings
			if (value is CriteriaOperator) {
				storage.Condition = (CriteriaOperator)value;
			}
			else{
				storage.Value = (T)value;
			}		

			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

			return true;
		}

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
