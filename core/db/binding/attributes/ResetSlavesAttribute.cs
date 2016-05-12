using System;
using DevExpress.XtraDataLayout;

namespace xwcs.core.db.binding.attributes
{
	[AttributeUsage(AttributeTargets.Property,	AllowMultiple = true)]
	public class ResetSlavesAttribute : CustomAttribute
	{
		public string[] Slaves { get; set; }	

		public override bool Equals(object obj)
		{
			ResetSlavesAttribute o = obj as ResetSlavesAttribute;
			if(o != null) {
				return Slaves == o.Slaves;
			}
            return false;
		}

		public override int GetHashCode()
		{
			int multiplier = 23;
			if (hashCode == 0)
			{
				int code = 133;
				code = multiplier * code + Slaves.GetHashCode();
				hashCode = code;
			}
			return hashCode;
		}
		public ResetSlavesAttribute(params string[] vals) {
			Slaves = vals;
		}
	}	
}
