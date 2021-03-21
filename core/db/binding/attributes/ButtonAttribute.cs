using System;
using DevExpress.XtraDataLayout;
using DevExpress.XtraEditors;
using xwcs.core.manager;
using DevExpress.XtraEditors.Repository;

namespace xwcs.core.db.binding.attributes
{
	[AttributeUsage(AttributeTargets.Property,	AllowMultiple = true)]
	public class ButtonAttribute : CustomAttribute
	{
		StyleController _styleController = new StyleController();
        private RepositoryItemButtonEdit _rle;
        private IDataBindingSource _src;
        private FieldRetrievedEventArgs _FieldRetrievedEventArgs;
        public ButtonAttribute()
		{
		}

		public override void applyRetrievingAttribute(IDataBindingSource src, FieldRetrievingEventArgs e)
		{
			e.EditorType = typeof(ButtonEdit);
		}

		public override void applyRetrievedAttribute(IDataBindingSource src, FieldRetrievedEventArgs e) 
		{
            _rle = (e.RepositoryItem as RepositoryItemButtonEdit);
            _FieldRetrievedEventArgs = e;
            _src = src;
            _rle.ButtonClick += Rle_ButtonClick;
            
		}

        private void Rle_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            Object tagSave = e.Button.Tag;
            e.Button.Tag = _FieldRetrievedEventArgs;
            if (!ReferenceEquals(_src, null)) _src.EditorsHost.onButtonEditClick(sender, e);
            e.Button.Tag=tagSave;
        }

        public override void unbind(IDataBindingSource src)
        {
            if (!ReferenceEquals(_rle, null)) _rle.ButtonClick -= Rle_ButtonClick;
            base.unbind(src);
        }
    }	
}