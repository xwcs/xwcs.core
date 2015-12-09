using System.ComponentModel;

namespace xwcs.core.evt
{
    public class EventProxy
    {
        protected EventHandlerList listEventDelegates = new EventHandlerList();
        public delegate void EventHandler(Event e);

        public void addEventHandler(object type, EventHandler value)
        {
            listEventDelegates.AddHandler(type, value);
        }

        public void removeEventHandler(object type, EventHandler value)
        {
            listEventDelegates.RemoveHandler(type, value);
        }

        public void fireEvent(Event e)
        {
            EventHandler handler = (EventHandler)listEventDelegates[e.type];
            if (handler != null)
            {
                handler(e);
            }
        }
    }
}
