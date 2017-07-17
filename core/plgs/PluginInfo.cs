using System;
using System.Collections.Generic;


namespace xwcs.core.plgs
{
    public enum PluginKind { PLGT_undef = 0, PLGT_nonvisual, PLGT_visual };
    public enum PluginAbility { PLGABLT_undef = 0, PLGABLT_submenu, PLGABLT_mainmenu, PLGABLT_toolbar, PLGABLT_usercontrol };

   

    public class PluginInfo 
    {
        //Private
        private Type _pluginType;
        private string _version;
        private PluginKind _kind;
        private Dictionary<PluginAbility, bool> _abilities = null;
        private Dictionary<Guid, xwcs.core.controls.VisualControlInfo> _controls = null;
        private Dictionary<Guid, xwcs.core.controls.WidgetDescriptor> _widgets = null;

        //Public getters, setters
        public string Name
        {
            get { return _pluginType.FullName; }
        }

        public string Version
        {
            get { return _version; }
        }

        public PluginKind Kind
        {
            get { return _kind; }
        }

        public Dictionary<PluginAbility, bool> Abilities
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
        public PluginInfo(Type pt, string version, PluginKind kind)
        {
            _pluginType = pt;
            _version = version;
            _kind = kind;
        }
        
		public string Namespace {
			get {
                return _pluginType.Namespace;
			}
		}
        
        

        //Public functions
        
        public void addAbility(PluginAbility ability)
        {
            if (_abilities == null) _abilities = new Dictionary<PluginAbility, bool>();
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
            if (_widgets.ContainsKey(guid)) return _widgets[guid];
            return null;            
        }

        public xwcs.core.controls.VisualControlInfo getVisualControlInfoByGuid(Guid guid)
        {
            if (_controls.ContainsKey(guid))  return _controls[guid];
            return null;
        }
    }
}
