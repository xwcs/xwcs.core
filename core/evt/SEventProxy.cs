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

        /* Multi thread synchronization */
        private static ISynchronizeInvoke _invokeDelegate = null;
        public static ISynchronizeInvoke InvokeDelegate
        {
            get
            {
                return _invokeDelegate;
            }
            set
            {
                if(value is System.Windows.Forms.Form)
                {
                    _invokeDelegate = value;
                    return;
                }

                throw new ApplicationException("Invokation delegate must be a Form!");                
            }
        }

        public static bool InvokeOnUIThread(Delegate what, object[] args)
        {
            if (!ReferenceEquals(_invokeDelegate, null))
            {
                if((_invokeDelegate is System.Windows.Forms.Form) && !(_invokeDelegate as System.Windows.Forms.Form).IsDisposed)
                {
                    if (_invokeDelegate.InvokeRequired)
                    {
                        _invokeDelegate.BeginInvoke(what, args);
                        return true;
                    }
                }                

                return false;
            }

            throw new ApplicationException("Missing Invocation delegate!");
        }


        /* globally allow and disallow events */
        static private Dictionary<Type, int> _blockedEvents = new Dictionary<Type,int>();
        public static void BlockEventTypes(Type[] events)
        {
            foreach (Type ea in events)
                BlockEventType(ea);
        }
        public static void BlockEventType(Type ea)
        {
            if (_blockedEvents.ContainsKey(ea))
            {
                _blockedEvents[ea]++;
            }
            else
            {
                _blockedEvents[ea] = 1;
            }

        }
        public static void BlockModelEvents()
        {
            BlockEventType(typeof(db.ModelPropertyChangedEventArgs));
        }

        public static void BlockAllChangeEvents()
        {
            BlockEventTypes(new Type[] { typeof(db.ModelPropertyChangedEventArgs), typeof(PropertyChangedEventArgs) });
        }

        public static void AllowEventTypes(Type[] events)
        {
            foreach (Type ea in events)
                AllowEventType(ea);
        }
        public static void AllowEventType(Type ea)
        {
            if (_blockedEvents.ContainsKey(ea))
            {
                if (_blockedEvents[ea] > 1)
                {
                    _blockedEvents[ea]--;
                }
                else
                {
                    _blockedEvents.Remove(ea);
                }
            }
        }
        public static void AllowModelEvents()
        {
            AllowEventType(typeof(db.ModelPropertyChangedEventArgs));
        }

        public static void AllowAllChangeEvents()
        {
            AllowEventTypes(new Type[] { typeof(db.ModelPropertyChangedEventArgs), typeof(PropertyChangedEventArgs) });
        }

        public static bool CanFireEvent(Type ea)
        {
            return _blockedEvents.Count == 0 || !_blockedEvents.ContainsKey(ea);
        }
    }
}
