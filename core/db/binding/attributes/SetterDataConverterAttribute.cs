using System;
using DevExpress.XtraDataLayout;
using DevExpress.XtraEditors;

namespace xwcs.core.db.binding.attributes
{
	[AttributeUsage(AttributeTargets.Property)]
	public class SetterDataConverterAttribute : CustomConverterAttribute
    {
        private string[] _from = new string[] { "" };
        private string _to = null;


        public SetterDataConverterAttribute(){}
        public SetterDataConverterAttribute(string to, params string[] from)
        {
            this._from = from;
            this._to = to;
        }

        public override bool isCompatible(Type t)
        {
            return t.IsAssignableFrom(typeof(string));
        }

        // getter time convert just return what we have
        public override object getConvert(object val)
        {
            return val;
        }
        // setter time convert if we match any of pattern convert   
        public override object setConvert(object val)
        {
            if (Array.IndexOf(_from, val) > -1) return _to;
            // get original back
            return val;
        }
    }	
}
