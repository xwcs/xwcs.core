using System;
using System.Xml.Serialization;
using System.Text;
using System.IO;
using System.Xml;
using xwcs.core.db.model.serialize;

namespace xwcs.core.db.model.attributes
{
	public enum SerializeKind
	{
		Undef = 0,
		XmlSerialization = 1,
		JsonSerialization = 2
	}

	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class SerializedAttribute : Attribute
	{
		public SerializeKind Kind { get; set; } = SerializeKind.Undef;
		public string DestinationPropertyName { get; set; } = "";	
	}

	static class ExtensionMethods
	{
		/*
			this method serialize some object into xml with full type name in 
			__content_type__="<object type>" attribute
			so we can reread it later and it use also specific root element name
		*/
		public static string TypedSerialize(this object objectInstance, string objectName, SerializeKind kind = SerializeKind.XmlSerialization)
		{
			if (objectInstance == null) return null;

			Type oType = objectInstance.GetType();
            if (kind == SerializeKind.XmlSerialization) {
				
				XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
				ns.Add("", "");
				var serializer = new XmlSerializer(oType, new XmlRootAttribute(objectName));
				XmlWriterSettings settings = new XmlWriterSettings();
				settings.Indent = false;
				settings.OmitXmlDeclaration = true;
				settings.Encoding = Encoding.UTF8;
				StringWriter sw = new StringWriter();
				serializer.Serialize(new XmlWriterExt(XmlWriter.Create(sw, settings), oType, objectName, "__content_type__"), objectInstance, ns);
				return sw.ToString();
			}
			else {
				return objectInstance.ToString();
            }
		}

		public static object TypedDeserialize(this string objectData, string objectName, SerializeKind kind = SerializeKind.XmlSerialization)
		{
			if (objectData == null || objectData.Length == 0) return null;

			if (kind == SerializeKind.XmlSerialization)
			{
				try {
					string nsVal = "";
					using (XmlReader reader = XmlReader.Create(new StringReader(objectData)))
					{
						reader.MoveToContent();

						if (reader.NodeType == XmlNodeType.Element && reader.Name == objectName)
						{
							reader.MoveToAttribute("__content_type__");
							nsVal = reader.Value;
						}
					}
					//de-serialize
					XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
					ns.Add("typename", nsVal);
					Type tt;
					if (xwcs.core.plgs.SPluginsLoader.getInstance().TryFindType(nsVal, out tt))
					{
						XmlSerializer s = new XmlSerializer(tt, new XmlRootAttribute(objectName));

						using (XmlReader reader = XmlReader.Create(new StringReader(objectData)))
						{
							return s.Deserialize(reader);
						}
					}
					else {
						return null;
					}
				}
				catch(Exception) {
					return null;
				}				
			}
			else {
				return null;
			}
		}
	}	
}