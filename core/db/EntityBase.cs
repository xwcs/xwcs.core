using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace xwcs.core.db
{
	using model;
	using model.attributes;
	using System.Collections.Generic;
	public class PropertyDeserialized : EventArgs
	{
		public PropertyDeserialized(object property, string propertyName)
		{
			SourceProperty = property;
			SourcePropertyName = propertyName;
		}
		public object SourceProperty { get; private set; }
		public string SourcePropertyName { get; private set; }
	}

	public delegate void PropertyDeserializedEventHandler(EntityBase sender, PropertyDeserialized e);


	[TypeDescriptionProvider(typeof(HyperTypeDescriptionProvider))]
	public abstract class EntityBase : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;	
		
		protected bool SetProperty<VT>(ref VT storage, VT value, [CallerMemberName] string propertyName = null)
		{
			if (Equals(storage, value)) return false;
			storage = value;

			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}

			return true;
		}

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}

	public abstract class SerializedEntityBase : EntityBase
	{
		
		public event PropertyDeserializedEventHandler PropertyDeserialized;

		public SerializedEntityBase()
		{

		}

		

		public abstract void GetMutablePropertiesType(Dictionary<string, Type> dest);

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
			if (storage == null && source != null && source.Length > 0)
			{
				storage = source.TypedDeserialize(sourcePropertyName, SerializeKind.XmlSerialization);
			}
			else
			if (storage == null && (source == null || source.Length == 0))
			{
				storage = null;
			}

			if (PropertyDeserialized != null)
			{
				PropertyDeserialized(this, new PropertyDeserialized(storage, propertyName));
			}

			return storage;
		}

		public abstract void DeserializeFields();
		public abstract void SerializeFields();
	}
}
