using System;
using DevExpress.XtraDataLayout;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Columns;
using System.Linq;
using DevExpress.XtraEditors.Filtering;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.DXErrorProvider;

namespace xwcs.core.db.binding.attributes
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class CheckValidAttribute : CustomAttribute
	{
        private RepositoryItem _ri;
        private IDataBindingSource _bs;
        private string _fn;
        // data layout like container
        public override void applyRetrievedAttribute(IDataBindingSource src, FieldRetrievedEventArgs e)
		{
            _bs = src;
            _ri = e.RepositoryItem;
            _ri.Validating += Rle_Validating;
            _fn = e.FieldName;
		}

        private void Rle_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            IValidableEntity ei = _bs.Current as IValidableEntity;
            if(ei != null)
            {
                Problem pr = ei.ValidateProperty(_fn);
                if (pr.Kind != ProblemKind.None)
                {
                    // we dont have valid data
                    if(sender is DevExpress.XtraEditors.BaseEdit)
                    {
                        (sender as DevExpress.XtraEditors.BaseEdit).ErrorText = pr.ErrorMessage;
                    }
                    e.Cancel = true;
                }
            }
        }

        public override void unbind(IDataBindingSource src) {
            // in case of grid source _ri is not set
            if (!ReferenceEquals(_ri, null))
                _ri.Validating -= Rle_Validating;
        }
    }
}