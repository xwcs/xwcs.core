using System;
using DevExpress.XtraDataLayout;
using DevExpress.XtraEditors;

namespace xwcs.core.db.binding.attributes
{
	[AttributeUsage(AttributeTargets.Property)]
	public class CustomConverterAttribute : Attribute
    {
        // getter time convert
        public virtual object getConvert(object val) 
        {
            return val;
        }
        // setter time convert    
        public virtual object setConvert(object val)
        {
            return val;
        }

        public virtual bool isCompatible(Type t)
        {
            return true;
        }
    }	
}
