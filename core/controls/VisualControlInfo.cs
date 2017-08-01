using System;
using System.Reflection;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace xwcs.core.controls
{
    public enum ControlDockStyle { PLGT_undef = 0, PLGT_document, PLGT_status, PLGT_property, PLGT_widget };

	[DataContract]
	public class VisualControlInfo
    {
        //Private
        private string _name;
        private string _version;
        private ControlDockStyle _dockStyle;
        private Guid _GUID;
        private Type _classType;
		private bool _allowMulti;

		/// <summary>
		/// This is Visual control unique instance GUID it will be used for 
		/// saved state management and distinction of single control instances
		/// NOTE: if this GUID is the same as _GUID field it means 
		///		  this info is template info, it cant be hold be no one control
		///		  each time there is new control done this guid must be regenerated
		/// </summary>
		private Guid _instance_GUID;


		/// <summary>
		/// This function make new instance of visual control
		/// </summary>
		/// <returns>IVisualControl</returns>
		public IVisualControl createInstance()
		{
            try
            {
                return ((IVisualControl)Activator.CreateInstance(_classType, new object[] { new VisualControlInfo(this) }));
            }catch(Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Error: " + manager.SLogManager.GetExceptionString(ex));
            }
            return null;		
		}

		/// <summary>
		/// This function restore VisualControl instance
		/// </summary>
		/// <returns>IVisualControl</returns>
		public IVisualControl restoreInstance()
		{
            try
            {
                return ((IVisualControl)Activator.CreateInstance(_classType, new object[] { this }));
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Error: " + manager.SLogManager.GetExceptionString(ex));
            }
            return null;
        }

		/// <summary>
		/// Serialization constructor
		/// </summary>
		public VisualControlInfo() { }

		/// <summary>
		/// Copy constructor, it will create instance guid!!!!
		/// </summary>
		/// <param name="src"></param>
		public VisualControlInfo(VisualControlInfo src)
		{
			_name = src._name;
			_version = src._version;
			_dockStyle = src._dockStyle;
			_GUID = src._GUID;
			_classType = src._classType;
			_allowMulti = src._allowMulti;
			// make new instance guid if we do coy from template
			_instance_GUID = Guid.NewGuid();
		}

		public VisualControlInfo(string name, Type t)
		{
			Name = name;
			
			_GUID = Guid.Parse((string)t.GetField("GUID", BindingFlags.Static | BindingFlags.Public).GetValue(null));
			FieldInfo fi = t.GetField("ALLOW_MULTI", BindingFlags.Static | BindingFlags.Public);
			_allowMulti = fi != null ? (bool)fi.GetValue(false) : false;
			_version = (string)t.GetField("VERSION", BindingFlags.Static | BindingFlags.Public).GetValue(null);
			_dockStyle = (ControlDockStyle)t.GetField("DOCK_STYLE", BindingFlags.Static | BindingFlags.Public).GetValue(null);
			_classType = t;
			_instance_GUID = _GUID; // this is template info
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

		[DataMember]
        public string TypeFullName
        {
            get { return _classType.FullName;  }
            set { _classType = GetType(value); }
        }

		[DataMember]
		public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

		[DataMember]
		public string Version
        {
            get { return _version; }
            set { _version = value; }
        }

		[DataMember]
		public ControlDockStyle DockStyle
        {
            get { return _dockStyle; }
            set { _dockStyle = value; }
        }

		[DataMember]
		public Guid InstanceGUID {
			get { return _instance_GUID;  }
			set { _instance_GUID = value; }
		}

		[DataMember]
		public Guid GUID
        {
			get { return _GUID; }
			set { _GUID = value; }
		}

		[DataMember]
		public bool AllowMulti 
		{
			get { return _allowMulti; }
			set { _allowMulti = value; }
		}

        public Type ClassType
        {
            get{ return _classType; }
			protected set { _classType = value; }
        }		
    }
}
