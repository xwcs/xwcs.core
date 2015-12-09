using System;

namespace xwcs.core.evt
{
    public class GenericEvent : Event
    {
        public GenericEvent(object sender, object data) : base(sender, EventType.GenericEvent, data)
        {
        }
    }
}
