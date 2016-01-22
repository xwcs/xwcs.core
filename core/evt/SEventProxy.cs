using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace xwcs.core.evt
{
    public class SEventProxy
    {

        private static SEventProxy instance;

        //singleton need private ctor
        private SEventProxy(){}

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static SEventProxy getInstance()
        {
            if (instance == null)
            {
                instance = new SEventProxy();
            }
            return instance;
        }


        /****

            MAIN methods
        */


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
