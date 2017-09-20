using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using xwcs.core.db.binding;


namespace xwcs.core.ui.db
{
	public class ControlMeta
	{
		public bool ReadOnly = false;
	}

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

	public interface IBehaviorContainer
	{
		DevExpress.Utils.Behaviors.BehaviorManager BehaviorMan { get; }
	}


	public interface IFormSupport
    {
        void RegisterAction(DynamicFormAction a);
        void RegisterActionTrigger(DynamicFormActionTrigger a);
        void AddBindingSource(IDataBindingSource bs);
        Control FindControlByPropertyName(string name);
        Dictionary<BaseEdit, IStyleController> DefaultStyles { get; }
		Dictionary<BaseEdit, ControlMeta> ControlsMeta { get; }

		Control Parent { get; }
		IBehaviorContainer BehaviorContainer { get; }

	}
    

    public class DynamicFormAction
    {
        private DynamicFormActionType _type;
        private string _fieldName;
        private WeakReference _control;
        private object _param;
        private WeakReference _bs;


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

        public IDataBindingSource BindingSource
        {
            get
            {
                if (_bs.IsAlive)
                {
                    return _bs.Target as IDataBindingSource;
                }
                return null;
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

        public DynamicFormAction(DynamicFormActionType t, string fn, object p, Control cnt, IDataBindingSource bs)
        {
            _type = t;
            _fieldName = fn;
            _control = new WeakReference(cnt);
            _param = p;
            _bs = new WeakReference(bs);
        }
    }

    public class DynamicFormActionTrigger
    {
        private DynamicFormActionType _type;
        private string _fieldName;
        private WeakReference _control;
        private object _param;
        private WeakReference _bs;

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

        public IDataBindingSource BindingSource
        {
            get
            {
                if (_bs.IsAlive)
                {
                    return _bs.Target as IDataBindingSource;
                }
                return null;
            }
        }

        public DynamicFormActionTrigger(DynamicFormActionType t, string fn, object p, Control cnt, IDataBindingSource bs)
        {
            _type = t;
            _fieldName = fn;
            _control = new WeakReference(cnt);
            _param = p;
            _bs = new WeakReference(bs);
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
        private Dictionary<string, List<DynamicFormActionTrigger>> _triggers = new Dictionary<string, List<DynamicFormActionTrigger>>();

        public List<DynamicFormActionTrigger> this[string tag]
        {
            get
            {
                if (!_triggers.ContainsKey(tag))
                {
                    _triggers[tag] = new List<DynamicFormActionTrigger>();
                }
                return _triggers[tag];
            }
        }
        
        public List<List<DynamicFormActionTrigger>> AllTriggerLists()
        {
            return _triggers.Values.ToList();
        }

        public List<List<DynamicFormActionTrigger>> AllTriggerListsByPattern(string tag)
        {
            return (List < List < DynamicFormActionTrigger >> )_triggers.GetItemsByKeyPattern(tag);
        }
    }



}
