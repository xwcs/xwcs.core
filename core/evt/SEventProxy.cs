using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace xwcs.core.evt
{	

	public class SEventProxy
    {
		internal class EventSource<TEvent> where TEvent : IEvent {
			public static readonly ConstructorInfo constructor = typeof(EventSource<TEvent>).GetConstructor(new Type[] { });

			public EventSource() { }

			private readonly WeakEventSource<TEvent> _eventSource = new WeakEventSource<TEvent>();
			public event EventHandler<TEvent> Event
			{
				add { _eventSource.Subscribe(value); }
				remove { _eventSource.Unsubscribe(value); }
			}
			public void Fire(TEvent evt) {
				_eventSource.Raise(evt.Sender, evt);
			}
		}


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

		private readonly Dictionary<object, object> _eventSources = new Dictionary<object, object>();


        /****
            MAIN methods
        */


        public void addEventHandler<T>(object type, EventHandler<T> value) where T : IEvent
		{
			if (!_eventSources.ContainsKey(type)){
				_eventSources[type] = EventSource<T>.constructor.Invoke(new object[] { });
			}
			(_eventSources[type] as EventSource<T>).Event += value;	
        }

		
		public void removeEventHandler<T>(object type, EventHandler<T> value) where T : IEvent
		{
			if (_eventSources.ContainsKey(type))
			{
				(_eventSources[type] as EventSource<T>).Event -= value;
			}
		}
		

		public void fireEvent<T>(T e) where T : IEvent
		{
			if (_eventSources.ContainsKey(e.Type))
			{
				(_eventSources[e.Type] as EventSource<T>).Fire(e);
			}
		}
    }
}
