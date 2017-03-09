using System;
using DevExpress.XtraDataLayout;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Views.Grid;
using xwcs.core.ui.db;

namespace xwcs.core.db.binding.attributes
{
    /*
     * THis attribute will mark field for some future dynamic changes on form
     * it need ACTION tag and eventual PARAM,
     * for first instance we will handle
     * MaskedEnable:
     *      ACTION: MaskedEnable
     *      PARAM: 64bit mask => if controll have correct mask it will remain enabed
     */
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DynamicAspectAttribute : CustomAttribute
    {
        protected DynamicFormActionType _Action;
        protected object _Param;


        public override bool Equals(object obj)
        {
            DynamicAspectAttribute o = obj as DynamicAspectAttribute;
            if (o != null)
            {
                return Action == o.Action && Param == o.Param;
            }
            return false;
        }

        public override int GetHashCode()
        {
            int multiplier = 23;
            if (hashCode == 0)
            {
                int code = 133;
                code = multiplier * code + (int)Action;
                code = multiplier * code + Param.GetHashCode();
                hashCode = code;
            }
            return hashCode;
        }


        public override void applyRetrievingAttribute(IDataBindingSource src, FieldRetrievingEventArgs e)
        {
        }

        public override void applyRetrievedAttribute(IDataBindingSource src, FieldRetrievedEventArgs e)
        {
            src.EditorsHost.FormSupport.RegisterAction(_Action, new DynamicFormAction(e.FieldName, _Param, null));
        }

        // grid like container
        public override void applyGridColumnPopulation(IDataBindingSource src, GridColumnPopulated e)
        {
        }
        public override void applyCustomRowCellEdit(IDataBindingSource src, CustomRowCellEditEventArgs e)
        {
        }
        public override void applyCustomEditShown(IDataBindingSource src, ViewEditorShownEventArgs e)
        {
        }       

        public DynamicFormActionType Action
        {
            get { return _Action; }
            set { _Action = value; }
        }

        public object Param
        {
            get
            {
                return _Param;
            }

            set
            {
                _Param = value;
            }
        }

        
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class MaskedEnable : DynamicAspectAttribute
    {
        public MaskedEnable(params object[] vals)
        {
            int tmp = 0;

            _Action = DynamicFormActionType.MaskedEnable;

            foreach (object v in vals)
            {
                tmp |= (int)v;
            }

            _Param = tmp;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class MaskedVisible : DynamicAspectAttribute
    {
        public MaskedVisible(params object[] vals)
        {
            int tmp = 0;

            _Action = DynamicFormActionType.MaskedVisible;

            foreach (object v in vals)
            {
                tmp |= (int)v;
            }

            _Param = tmp;
        }
    }
}
