using System;

namespace xwcs.core.evt
{
    public class Event : EventArgs, IEvent
    {
        private object _sender;
        private object _type;
        protected object _data;

        public Event(object s, object t, object d)
        {
            _sender = s;
            _type = t;
            _data = d;
        }

        public object Sender
        {
            get { return _sender; }
            set { _sender = value; }
        }

        public object Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public object Data
        {
            set { _data = value; }
            get { return _data; }
        }
    }
}
