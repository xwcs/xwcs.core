using System;
using System.Collections.Generic;
using System.Xml.Serialization;


namespace xwcs.ui.controls
{
    public enum controlType { PLGT_undef = 0, PLGT_document, PLGT_status, PLGT_info };

    public class ControlInfo
    {
        //Private
        private string _name;
        private string _version;
        private controlType _type;
        private Guid _GUID;

        //Public getters, setters
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                _name = value;
            }
        }

        public string Version
        {
            get
            {
                return _version;
            }

            set
            {
                _version = value;
            }
        }

        public controlType Type
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

        public Guid GUID
        {
            get
            {
                return _GUID;
            }

            set
            {
                _GUID = value;
            }
        }
    }
}
