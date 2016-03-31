using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace xwcs.core.db
{
	using model;
	using model.attributes;

	public abstract class EntityBase<T> : NotifyStructureChanged<T>, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;		

		public EntityBase() {
			// handle type registering
			// we have to unbox from eventual entity proxy
			Type t = GetType();

			if (t.BaseType != null && t.Namespace == "System.Data.Entity.DynamicProxies")
			{
				HyperTypeDescriptionProvider.Add(t.BaseType);
			}
			else
			{
				HyperTypeDescriptionProvider.Add(t);
			}
		}

		protected bool SetProperty<VT>(ref VT storage, VT value, [CallerMemberName] string propertyName = null)
		{
			if (Equals(storage, value)) return false;
			storage = value;
			OnPropertyChanged(propertyName);
			return true;
		}

		// return string : we will dump object to string, but we will do it only if source is not NULL
		// we cant reset value using object, this can be reset just setting empty string in dump property
		protected string SerializeAndGet(object source, ref string storage, [CallerMemberName] string propertyName = null)
		{
			if (storage is string && source != null)
			{
				storage = source.TypedSerialize(propertyName, SerializeKind.XmlSerialization);
			}
			return storage;
		}

		// return object : we will de-serialize string into object but only if there is not de-serialized other one
		// cause we use lazy de-serializing, we do this just first time called
		protected object GetOrDeserialize(string source, string sourcePropertyName, ref object storage, [CallerMemberName] string propertyName = null)
		{
			if(storage == null && source != null && source.Length > 0) {
				storage = source.TypedDeserialize(sourcePropertyName, SerializeKind.XmlSerialization);
			}else 
			if (storage == null && (source == null || source.Length == 0)){
				storage = null;
			}
			CheckType(storage, propertyName);
			return storage;	
		}

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public virtual void DeserializeFields()
		{
		}
		public virtual void SerializeFields()
		{
		}
	}	
}
