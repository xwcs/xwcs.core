using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xwcs.core.controls
{
    public class WidgetDescriptor
    {
        private string _widgetName;
        private string _description;
        private Guid _guidNormalControl;
        private Guid _guidMaxControl;
        private bool _visible;
        private Guid _guid;



        public string WidgetName
        {
            get
            {
                return _widgetName;
            }

            set
            {
                _widgetName = value;
            }
        }

        public string Description
        {
            get
            {
                return _description;
            }

            set
            {
                _description = value;
            }
        }

        public Guid GuidNormalControl
        {
            get
            {
                return _guidNormalControl;
            }

            set
            {
                _guidNormalControl = value;
            }
        }

        public Guid GuidMaxControl
        {
            get
            {
                return _guidMaxControl;
            }

            set
            {
                _guidMaxControl = value;
            }
        }

        public bool Visible
        {
            get
            {
                return _visible;
            }

            set
            {
                _visible = value;
            }
        }

        public Guid GUID
        {
            get
            {
                return _guid;
            }

            set
            {
                _guid = value;
            }
        }

        
    }
}
