using System;

namespace xwcs.core.evt
{
    public class GenericEvent : EventData
    {
        public GenericEvent(object sender, object data) : base(sender, EventType.GenericEvent, data)
        {
        }
    }
}
