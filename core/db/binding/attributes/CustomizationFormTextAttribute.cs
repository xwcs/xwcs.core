using System;
using DevExpress.XtraDataLayout;
using DevExpress.XtraEditors;

namespace xwcs.core.db.binding.attributes
{
	[AttributeUsage(AttributeTargets.Property,	AllowMultiple = true)]
	public class CustomizationFormTextAttribute : CustomAttribute
	{
        private string _value = "";

        public CustomizationFormTextAttribute(string value = "")
        {
            _value = value;
        }

        public override void applyRetrievedAttribute(IDataBindingSource src, FieldRetrievedEventArgs e) 
		{
            if(_value.Length > 0)
            {
                e.Item.CustomizationFormText = _value;
            }
        }       
    }	
}