using System;
using System.Collections.Generic;


namespace xwcs.core.plgs
{
    public enum pluginType { PLGT_undef = 0, PLGT_nonvisual, PLGT_visual };
    public enum pluginAbility { PLGABLT_undef = 0, PLGABLT_submenu, PLGABLT_mainmenu, PLGABLT_toolbar, PLGABLT_usercontrol };

    public class PluginInfo
    {
        //Private
        private string _name;
        private string _version;
        private pluginType _type;
        private Dictionary<pluginAbility, bool> _abilities = null;
        private Dictionary<string, Guid> _controls = null;

        //Public getters, setters
        public string name
        {
            get { return _name; }
            set { _name = name; }
        }

        public string version
        {
            get { return _version; }
            set { _version = value;  }
        }

        public pluginType type
        {
            get { return _type; }
            set { _type = value; }
        }

        public Dictionary<pluginAbility, bool> abilities
        {
            get { return _abilities; }
            set { _abilities = value; }
        }

        public Dictionary<string, Guid> controls
        {
            get { return _controls;  }
            set { _controls = value; }
        }

        //Constructors
        public PluginInfo(string name, string version, pluginType type)
        {
            _name = name;
            _version = version;
            _type = type;
        }
        
        public PluginInfo(string name, string version, pluginType type, Dictionary<pluginAbility, bool> abilities, Dictionary<string, Guid> controls)
        {
            _name = name;
            _version = version;
            _type = type;
            _abilities = abilities;
            _controls = controls;
        }
        

        //Public functions
        public Guid getGuidControlByName(string name)
        {
            if (controls.ContainsKey(name)) return controls[name];
            return Guid.Empty;
        }

        public void addAbility(pluginAbility ability)
        {
            if (_abilities == null) _abilities = new Dictionary<pluginAbility, bool>();
            abilities.Add(ability, true);
        }

        public void addControl(string name, Guid guid)
        {
            if (_controls == null) _controls = new Dictionary<string, Guid>();
            controls.Add(name, guid);
        }
    }
}
