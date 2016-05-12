using System;
using DevExpress.XtraDataLayout;

namespace xwcs.core.db.binding.attributes
{
	[AttributeUsage(AttributeTargets.Property,	AllowMultiple = true)]
	public class StyleAttribute : CustomAttribute
	{
		uint _backGrndColor;
		bool _backGrndColorUsed;

		public override bool Equals(object obj)
		{
			StyleAttribute o = obj as StyleAttribute;
			if(o != null) {
				return _backGrndColor == o._backGrndColor && _backGrndColorUsed == o._backGrndColorUsed;
			}
            return false;
		}

		public override int GetHashCode()
		{
			int multiplier = 23;
			if (hashCode == 0)
			{
				int code = 133;
				code = multiplier * code + (int)_backGrndColor;
				code = multiplier * code + (_backGrndColorUsed ? 1 : 0);
				hashCode = code;
			}
			return hashCode;
		}
		public StyleAttribute() {
			_backGrndColorUsed = false; //default
		}

		public uint BackgrounColor
		{
			get { return _backGrndColor; }
			set { _backGrndColorUsed = true;  _backGrndColor = value; }
		}

		public override void applyRetrievedAttribute(IDataBindingSource src, FieldRetrievedEventArgs e) {
			if (_backGrndColorUsed)
			{
				e.Control.Padding = new System.Windows.Forms.Padding(0);
				e.Control.BackColor = System.Drawing.Color.FromArgb((int)_backGrndColor);
			}
		}
	}	
}
