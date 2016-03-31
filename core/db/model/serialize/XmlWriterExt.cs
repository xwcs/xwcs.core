using System;
using System.Xml;

namespace xwcs.core.db.model.serialize
{
	public class XmlWriterExt : XmlWriter
	{
		private XmlWriter _root;
		Type _type;
		string _type_attribute_name = "__content_type__";
		string _root_element_name = "";      

		public XmlWriterExt(XmlWriter root, Type type, string rootElementName, string typeAttributeName = "__content_type__") 
		{ 
			_root = root; 
			_type = type;
			_type_attribute_name = typeAttributeName;
			_root_element_name = rootElementName;
        }

		public override WriteState WriteState
		{
			get
			{
				return _root.WriteState;
			}
		}

		public override void WriteStartAttribute(string prefix, string localName, string ns)
		{
			_root.WriteStartAttribute(prefix, localName, ns);
		}

		public override void WriteString(string text)
		{
			_root.WriteString(text);
		}

		public override void WriteEndAttribute()
		{
			_root.WriteEndAttribute();
		}

		public override void WriteStartDocument()
		{
			_root.WriteStartDocument();
		}

		public override void WriteStartDocument(bool standalone)
		{
			_root.WriteStartDocument(standalone);
		}

		public override void WriteEndDocument()
		{
			_root.WriteEndDocument();
		}

		public override void WriteDocType(string name, string pubid, string sysid, string subset)
		{
			_root.WriteDocType(name, pubid, sysid, subset);
		}

		public override void WriteStartElement(string prefix, string localName, string ns)
		{
			_root.WriteStartElement(prefix, localName, ns);
			if (localName == _root_element_name)
			{
				_root.WriteStartAttribute("", _type_attribute_name, "");
				_root.WriteString(_type.FullName);
				_root.WriteEndAttribute();
			}
		}

		public override void WriteEndElement()
		{
			_root.WriteEndElement();
		}

		public override void WriteFullEndElement()
		{
			_root.WriteFullEndElement();
		}

		public override void WriteCData(string text)
		{
			_root.WriteCData(text);
		}

		public override void WriteComment(string text)
		{
			_root.WriteComment(text);
		}

		public override void WriteProcessingInstruction(string name, string text)
		{
			_root.WriteProcessingInstruction(name, text);
		}

		public override void WriteEntityRef(string name)
		{
			_root.WriteEntityRef(name);
		}

		public override void WriteCharEntity(char ch)
		{
			_root.WriteCharEntity(ch);
		}

		public override void WriteWhitespace(string ws)
		{
			_root.WriteWhitespace(ws);
		}

		public override void WriteSurrogateCharEntity(char lowChar, char highChar)
		{
			_root.WriteSurrogateCharEntity(lowChar, highChar);
		}

		public override void WriteChars(char[] buffer, int index, int count)
		{
			_root.WriteChars(buffer, index, count);
		}

		public override void WriteRaw(char[] buffer, int index, int count)
		{
			_root.WriteRaw(buffer, index, count);
		}

		public override void WriteRaw(string data)
		{
			_root.WriteRaw(data);
		}

		public override void WriteBase64(byte[] buffer, int index, int count)
		{
			_root.WriteBase64(buffer, index, count);
		}

		public override void Flush()
		{
			_root.Flush();
		}

		public override string LookupPrefix(string ns)
		{
			return _root.LookupPrefix(ns);
		}
	}
}
