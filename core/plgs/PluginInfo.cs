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
        private Dictionary<Guid, xwcs.core.controls.VisualControlInfo> _controls = null;
        private Dictionary<Guid, xwcs.core.controls.WidgetDescriptor> _widgets = null;

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

        public Dictionary<Guid, xwcs.core.controls.VisualControlInfo> Controls
        {
            get { return _controls;  }
        }

        public Dictionary<Guid, xwcs.core.controls.WidgetDescriptor> Widgets
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
        
        
        

        //Public functions
        
        public void addAbility(pluginAbility ability)
        {
            if (_abilities == null) _abilities = new Dictionary<pluginAbility, bool>();
            _abilities.Add(ability, true);
        }

        public void addControl(xwcs.core.controls.VisualControlInfo info)
        {
            if (_controls == null) _controls = new Dictionary<Guid, xwcs.core.controls.VisualControlInfo>();
            _controls.Add(info.GUID, info);
        }
        
        public void addWidget(xwcs.core.controls.WidgetDescriptor desc)
        {
            if (_widgets == null) _widgets = new Dictionary<Guid, xwcs.core.controls.WidgetDescriptor>();
            _widgets.Add(desc.GUID, desc);
        }

        public xwcs.core.controls.WidgetDescriptor getWidgetByGuid(Guid guid)
        {
            return _widgets[guid];            
            //TODO : return NULL if not exists
        }

        public xwcs.core.controls.VisualControlInfo getVisualControlInfoByGuid(Guid guid)
        {
            return _controls[guid];
            //TODO : return NULL if not exists
        }
    }
}
