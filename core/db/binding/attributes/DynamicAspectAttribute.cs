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
     * MaskedEnableTarget:
     *      ACTION: MaskedEnableTarget
     *      PARAM: 64bit mask => if controll have correct mask it will remain enabed
     */
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class DynamicAspectAttribute : CustomAttribute
    {
        protected DynamicFormActionElementType _elementType;
        protected DynamicFormActionType _action;
        protected object _param;


        public override bool Equals(object obj)
        {
            DynamicAspectAttribute o = obj as DynamicAspectAttribute;
            if (o != null)
            {
                return _elementType == o._elementType &&  _action == o._action && _param == o._param;
            }
            return false;
        }

        public override int GetHashCode()
        {
            int multiplier = 23;
            if (hashCode == 0)
            {
                int code = 133;
                code = multiplier * code + (int)_elementType;
                code = multiplier * code + (int)_action;
                code = multiplier * code + (_param != null ? _param.GetHashCode() : 0);
                hashCode = code;
            }
            return hashCode;
        }

        public override void applyRetrievedAttribute(IDataBindingSource src, FieldRetrievedEventArgs e)
        {
            if(_elementType == DynamicFormActionElementType.Action)
            {
                src.EditorsHost.FormSupport.RegisterAction(new DynamicFormAction(_action, e.FieldName, _param, null));
            }else
            {
                src.EditorsHost.FormSupport.RegisterActionTrigger(new DynamicFormActionTrigger(_action, e.FieldName, _param, null));
            }
            
        }



        public DynamicFormActionType Action
        {
            get { return _action; }
            set { _action = value; }
        }

        public object Param
        {
            get
            {
                return _param;
            }

            set
            {
                _param = value;
            }
        }

        protected DynamicFormActionElementType ElementType
        {
            get
            {
                return _elementType;
            }

            set
            {
               _elementType = value;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class MaskedEnableTarget : DynamicAspectAttribute
    {
        public MaskedEnableTarget(params object[] vals)
        {
            int tmp = 0;

            _action = DynamicFormActionType.MaskedEnable;
            _elementType = DynamicFormActionElementType.Action;

            foreach (object v in vals)
            {
                tmp |= (int)v;
            }

            _param = tmp;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class MaskedEnableTrigger : DynamicAspectAttribute
    {
        public MaskedEnableTrigger()
        {
            _action = DynamicFormActionType.MaskedEnable;
            _elementType = DynamicFormActionElementType.ActionTrigger;
            _param = null;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class MaskedVisibleTarget : DynamicAspectAttribute
    {
        public MaskedVisibleTarget(params object[] vals)
        {
            int tmp = 0;

            _action = DynamicFormActionType.MaskedVisible;
            _elementType = DynamicFormActionElementType.Action;

            foreach (object v in vals)
            {
                tmp |= (int)v;
            }

            _param = tmp;
        }
    }
}
