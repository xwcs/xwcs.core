using System;
using System.Reflection;
using System.Collections.Generic;
using System.Xml.Serialization;


namespace xwcs.core.controls
{
    public enum ControlDockStyle { PLGT_undef = 0, PLGT_document, PLGT_status, PLGT_property, PLGT_widget };

    public class VisualControlInfo
    {
        //Private
        private string _name;
        private string _version;
        private ControlDockStyle _dockStyle;
        private Guid _GUID;
        private Type _classType;


        public IVisualControl createInstance()
        {
            IVisualControl vc = ((IVisualControl)Activator.CreateInstance(_classType));
            vc.VisualControlInfo = this;
            return vc;
        }

        public static Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null) return type;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
        }

        public string TypeStr
        {
            get { return _classType.FullName;  }
            set {
                _classType = GetType(value);
            }
        }

        //Public getters, setters
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Version
        {
            get { return _version; }
            set { _version = value; }
        }

        public ControlDockStyle DockStyle
        {
            get { return _dockStyle; }
            set { _dockStyle = value; }
        }

        public Guid GUID
        {
            get { return _GUID; }
            set { _GUID = value; }
        }

        [XmlIgnore]
        public Type ClassType
        {
            get
            {
                return _classType;
            }

            set
            {
                _classType = value;
            }
        }

        public VisualControlInfo() {; }

        public VisualControlInfo(string name, Type t)
        {
            Name = name;
            
            _GUID = Guid.Parse( (string)t.GetField("GUID", BindingFlags.Static | BindingFlags.Public).GetValue(null) );
            _version = (string)t.GetField("VERSION", BindingFlags.Static | BindingFlags.Public).GetValue(null);
            _dockStyle = (ControlDockStyle)t.GetField("DOCK_STYLE", BindingFlags.Static | BindingFlags.Public).GetValue(null);
            
            _classType = t;            
        }
    }
}
