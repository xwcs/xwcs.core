using System;
using System.Collections.Generic;


namespace xwcs.core.plgs
{
    public enum pluginType { PLGT_undef = 0, PLGT_nonvisual, PLGT_visual };
    public enum pluginAbility { PLGABLT_undef = 0, PLGABLT_submenu, PLGABLT_mainmenu, PLGABLT_toolbar, PLGABLT_usercontrol };

    public struct WidgetDescriptor
    {
        public string name;
        public string descriptor;
        public Guid guidNormal;
        public Guid guidMax;

        public WidgetDescriptor(string name, string descriptor, Guid guidNormal, Guid guidMax)
        {
            this.name = name;
            this.descriptor = descriptor;
            this.guidNormal = guidNormal;
            this.guidMax = guidMax;
        }
    }    

    public class PluginInfo 
    {
        //Private
        private string _name;
        private string _version;
        private pluginType _type;
        private Dictionary<pluginAbility, bool> _abilities = null;
        private Dictionary<string, Guid> _controls = null;
        private Dictionary<string, WidgetDescriptor> _widgets = null;

        //Public getters, setters
        public string Name
        {
            get { return _name; }
        }

        public string Version
        {
            get { return _version; }
        }

        public pluginType Type
        {
            get { return _type; }
        }

        public Dictionary<pluginAbility, bool> Abilities
        {
            get { return _abilities; }
        }

        public Dictionary<string, Guid> Controls
        {
            get { return _controls;  }
        }

        public Dictionary<string, WidgetDescriptor> Widgets
        {
            get{ return _widgets; }
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
            if (Controls.ContainsKey(name)) return Controls[name];
            return Guid.Empty;
        }

        public void addAbility(pluginAbility ability)
        {
            if (_abilities == null) _abilities = new Dictionary<pluginAbility, bool>();
            Abilities.Add(ability, true);
        }

        public void addControl(string name, Guid guid)
        {
            if (_controls == null) _controls = new Dictionary<string, Guid>();
            Controls.Add(name, guid);
        }
        
        public void addWidget(WidgetDescriptor desc)
        {
            if (_widgets == null) _widgets = new Dictionary<string, WidgetDescriptor>();
            _widgets.Add(desc.name, desc);
        }

        public Guid getGuidNormalByName(string name)
        {
            if (_widgets.ContainsKey(name)) return _widgets[name].guidNormal;
            return Guid.Empty;
        }

        public Guid getGuidMaxByName(string name)
        {
            if (_widgets.ContainsKey(name)) return _widgets[name].guidMax;
            return Guid.Empty;
        }

        public WidgetDescriptor getWidgetByName(string name)
        {
            return _widgets[name];            
            //TODO : return NULL if not exists
        }
    }
}
