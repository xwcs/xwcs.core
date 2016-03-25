using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace xwcs.core.db
{
	public abstract class EntityBase : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
		{
			if (Equals(storage, value)) return false;
			storage = value;
			OnPropertyChanged(propertyName);
			return true;
		}
		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			var eventHandler = PropertyChanged;
			if (eventHandler != null)
			{
				eventHandler(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}	
}
