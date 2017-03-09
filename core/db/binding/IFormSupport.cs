using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using xwcs.core.db.binding;

namespace xwcs.core.ui.db
{
    public enum DynamicFormActionType
    {
        MaskedEnable = 0,
        MaskedVisible = 1
    }


    /*
    * Classes for Form support handling
    */
    public interface IFormSupport
    {
        void RegisterAction(DynamicFormActionType t, DynamicFormAction a);
    }
    

    public class DynamicFormAction
    {
        private string _FieldName;
        private WeakReference _Control;
        private object _Param;

        public string FieldName
        {
            get
            {
                return _FieldName;
            }
        }

        public Control Control
        {
            get
            {
                if (_Control.IsAlive)
                {
                    return (_Control.Target as Control);
                }
                return null;
            }
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

        public DynamicFormAction(string fn, object p, Control cnt)
        {
            _FieldName = fn;
            _Control = new WeakReference(cnt);
            _Param = p;
        }


    }

    public class DynamicFormActions
    {
        private Dictionary<DynamicFormActionType, List<DynamicFormAction>> _actions = new Dictionary<DynamicFormActionType, List<DynamicFormAction>>();

        public List<DynamicFormAction> this[DynamicFormActionType tag]
        {
            get
            {
                if (!_actions.ContainsKey(tag))
                {
                    _actions[tag] = new List<DynamicFormAction>();
                }
                return _actions[tag];
            }
        }
    }


    
}
