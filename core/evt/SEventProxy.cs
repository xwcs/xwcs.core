using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace xwcs.core.evt
{
	public delegate void EventHandler<in T>(T e) where T : Event;

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

        public void addEventHandler<T>(object type, EventHandler<T> value) where T : Event
		{
            listEventDelegates.AddHandler(type, value);
        }
		public void addEventHandler(object type, EventHandler<Event> value)
		{
			listEventDelegates.AddHandler(type, value);
		}

		public void removeEventHandler<T>(object type, EventHandler<T> value) where T : Event
		{
            listEventDelegates.RemoveHandler(type, value);
        }
		
		public void removeEventHandler(object type, EventHandler<Event> value)
		{
			listEventDelegates.RemoveHandler(type, value);
		}

		public void fireEvent<T>(T e) where T : Event
        {
			((EventHandler<T>)listEventDelegates[e.Type])?.Invoke(e);
		}
    }
}
