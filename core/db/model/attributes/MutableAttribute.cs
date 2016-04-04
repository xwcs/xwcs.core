using System;

namespace xwcs.core.db.model.attributes
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class MutableAttribute : Attribute
	{
	}
}
