using System;
using DevExpress.XtraDataLayout;
using DevExpress.XtraEditors;

namespace xwcs.core.db.binding.attributes
{
	[AttributeUsage(AttributeTargets.Property,	AllowMultiple = true)]
	public class StyleAttribute : CustomAttribute
	{
		StyleController _styleController = new StyleController();
		uint _backGrndColor;
		uint _backGrndColorDisabled;
		uint _backGrndColorFocused;
		uint _backGrndColorReadOnly;
		int _columnWidth = -1;
        int _columnMinWidth = -1;
        uint _halignment = 0; //Default = 0, Near = 1, Center = 2, Far = 3

        // separate styling

        public int ColumnMinWidth
        {
            get { return _columnMinWidth; } set { _columnMinWidth = value; }
        }


        public int ColumnWidth 
		{ 
			get { return _columnWidth; } 
			set { _columnWidth = value; }
		}


		public StyleAttribute()
		{
		}

		public uint BackgrounColor
		{
			get { return _backGrndColor; }
			set 
			{ 
				_backGrndColor = value;
				_styleController.Appearance.BackColor = System.Drawing.Color.FromArgb((int)_backGrndColor);				
			}
		}

		public uint BackGrndColorDisabled
		{
			get { return _backGrndColorDisabled; }
			set
			{
				_backGrndColorDisabled = value;
				_styleController.AppearanceDisabled.BackColor = System.Drawing.Color.FromArgb((int)_backGrndColorDisabled);
			}
		}

		public uint BackGrndColorFocused
		{
			get { return _backGrndColorFocused; }
			set
			{
				_backGrndColorFocused = value;
				_styleController.AppearanceFocused.BackColor = System.Drawing.Color.FromArgb((int)_backGrndColorFocused);
			}
		}

		public uint BackGrndColorReadOnly
		{
			get { return _backGrndColorReadOnly; }
			set
			{
				_backGrndColorReadOnly = value;
				_styleController.AppearanceReadOnly.BackColor = System.Drawing.Color.FromArgb((int)_backGrndColorReadOnly);
			}
		}

		public uint HAlignment
		{
			get { return _halignment; }
			set { _halignment = value; }
		}

       
        public override void applyRetrievedAttribute(IDataBindingSource src, FieldRetrievedEventArgs e) 
		{
            // register default style controller
            src.EditorsHost.FormSupport.DefaultStyles[(e.Control as BaseEdit)] = _styleController;
            (e.Control as BaseEdit).StyleController = _styleController;
        }

        public override void applyGridColumnPopulation(IDataBindingSource src, GridColumnPopulated e) {
            if (!ReferenceEquals(e.Column, null))
            {
                if (ColumnWidth != -1)
                {
                    //set column width
                    //FixedWidth must be set for change column's width runtime
                    e.Column.FixedWidth = true;
                    e.Column.Width = ColumnWidth;
                    e.Column.AppearanceCell.TextOptions.HAlignment = (DevExpress.Utils.HorzAlignment)_halignment;
                } 
                if (ColumnMinWidth >=0)
                {
                    e.Column.MinWidth = ColumnMinWidth;
                }
            }           
        }
    }	
}