using System.ComponentModel;

namespace xwcs.core.evt
{
    public class EventProxy
    {
        protected EventHandlerList listEventDelegates = new EventHandlerList();
        public delegate void EventHandler(EventData e);

        public void addEventHandler(object type, EventHandler value)
        {
            listEventDelegates.AddHandler(type, value);
        }

        public void removeEventHandler(object type, EventHandler value)
        {
            listEventDelegates.RemoveHandler(type, value);
        }

        public void fireEvent(EventData e)
        {
            EventHandler handler = (EventHandler)listEventDelegates[e.type];
            if (handler != null)
            {
                handler(e);
            }
        }
    }
}
