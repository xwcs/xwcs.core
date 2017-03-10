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

    public enum DynamicFormActionElementType
    {
        ActionTrigger = 0,
        Action = 1
    }

    /*
    * Classes for Form support handling
    */
    public interface IFormSupport
    {
        void RegisterAction(DynamicFormAction a);
        void RegisterActionTrigger(DynamicFormActionTrigger a);
        void AddBindingSource(IDataBindingSource bs);
    }
    

    public class DynamicFormAction
    {
        private DynamicFormActionType _type;
        private string _fieldName;
        private WeakReference _control;
        private object _param;

        public string FieldName
        {
            get
            {
                return _fieldName;
            }
        }

        public Control Control
        {
            get
            {
                if (_control.IsAlive)
                {
                    return (_control.Target as Control);
                }
                return null;
            }
            set
            {
                _control = new WeakReference(value);
            }
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

        public DynamicFormActionType ActionType
        {
            get
            {
                return _type;
            }

            set
            {
                _type = value;
            }
        }

        public DynamicFormAction(DynamicFormActionType t, string fn, object p, Control cnt)
        {
            _type = t;
            _fieldName = fn;
            _control = new WeakReference(cnt);
            _param = p;
        }
    }

    public class DynamicFormActionTrigger
    {
        private DynamicFormActionType _type;
        private string _fieldName;
        private WeakReference _control;
        private object _param;

        public string FieldName
        {
            get
            {
                return _fieldName;
            }
        }

        public Control Control
        {
            get
            {
                if (_control.IsAlive)
                {
                    return (_control.Target as Control);
                }
                return null;
            }
            set
            {
                _control = new WeakReference(value);
            }
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

        public DynamicFormActionTrigger(DynamicFormActionType t, string fn, object p, Control cnt)
        {
            _type = t;
            _fieldName = fn;
            _control = new WeakReference(cnt);
            _param = p;
        }

        public DynamicFormActionType ActionType
        {
            get
            {
                return _type;
            }

            set
            {
                _type = value;
            }
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

    public class DynamicFormActionTriggers
    {
        // key is field name
        private Dictionary<string, List<DynamicFormActionTrigger>> _actions = new Dictionary<string, List<DynamicFormActionTrigger>>();

        public List<DynamicFormActionTrigger> this[string tag]
        {
            get
            {
                if (!_actions.ContainsKey(tag))
                {
                    _actions[tag] = new List<DynamicFormActionTrigger>();
                }
                return _actions[tag];
            }
        }
        
    }



}
