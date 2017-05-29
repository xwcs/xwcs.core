/*
 *	Autor: Thomas Levesque
 *	link:  https://github.com/thomaslevesque/WeakEvent
 */
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace xwcs.core.evt
{
    public class WeakEventSource<TEventArgs>
    {
        private readonly List<WeakDelegate> _handlers;

        public WeakEventSource()
        {
            _handlers = new List<WeakDelegate>();
        }

        public void Raise(object sender, TEventArgs e)
        {
            int cnt = 10;
            while (cnt-- > 0)
                if (System.Threading.Monitor.TryEnter(_handlers, 5000))
                {
                    try
                    {
                        _handlers.RemoveAll(h => !h.Invoke(sender, e));

                        return; //done
                    }
                    finally
                    {
                        System.Threading.Monitor.Exit(_handlers);
                    }
                }

            throw new ApplicationException("Cant raise event!");
        }

        public void Subscribe(EventHandler<TEventArgs> handler)
        {
            SubscribeCore(handler);
        }
        public void Subscribe(EventHandler handler)
        {
            SubscribeCore(handler);
        }
        public void Subscribe(PropertyChangedEventHandler handler)
        {
            SubscribeCore(handler);
        }
        protected void SubscribeCore(object handler)
        {
            var weakHandlers = (handler as Delegate)
                .GetInvocationList()
                .Select(d => new WeakDelegate(d))
                .ToList();

            int cnt = 10;
            while (cnt-- > 0)
                if (System.Threading.Monitor.TryEnter(_handlers, 5000))
                {
                    try
                    {
                        _handlers.AddRange(weakHandlers);

                        return; //done
                    }
                    finally
                    {
                        System.Threading.Monitor.Exit(_handlers);
                    }
                }

            throw new ApplicationException("Cant register handler!");
        }

        

        public void Unsubscribe(EventHandler<TEventArgs> handler)
        {
            UnsubscribeCore(handler);
        }
        public void Unsubscribe(EventHandler handler)
        {
            UnsubscribeCore(handler);
        }
        public void Unsubscribe(PropertyChangedEventHandler handler)
        {
            UnsubscribeCore(handler);
        }
        protected void UnsubscribeCore(object handler)
        {
            int cnt = 10;
            while (cnt-- > 0)
                if (System.Threading.Monitor.TryEnter(_handlers, 5000))
                {
                    try
                    {
                        int index = _handlers.FindIndex(h => h.IsMatch(handler));
                        if (index >= 0)
                            _handlers.RemoveAt(index);

                        return; // done
                    }
                    finally
                    {
                        System.Threading.Monitor.Exit(_handlers);
                    }
                }

            throw new ApplicationException("Cant unregister handler!");
        }


        class WeakDelegate
        {
            #region Open handler generation and cache

            private delegate void OpenEventHandler(object target, object sender, TEventArgs e);

            // ReSharper disable once StaticMemberInGenericType (by design)
            private static readonly ConcurrentDictionary<MethodInfo, OpenEventHandler> _openHandlerCache =
                new ConcurrentDictionary<MethodInfo, OpenEventHandler>();

            private static OpenEventHandler CreateOpenHandler(MethodInfo method)
            {
                var target = Expression.Parameter(typeof(object), "target");
                var sender = Expression.Parameter(typeof(object), "sender");
                var e = Expression.Parameter(typeof(TEventArgs), "e");

                if (method.IsStatic)
                {
                    var expr = Expression.Lambda<OpenEventHandler>(
                        Expression.Call(
                            method,
                            sender, e),
                        target, sender, e);
                    return expr.Compile();
                }
                else
                {
                    var expr = Expression.Lambda<OpenEventHandler>(
                        Expression.Call(
                            Expression.Convert(target, method.DeclaringType),
                            method,
                            sender, e),
                        target, sender, e);
                    return expr.Compile();
                }
            }

            #endregion

            private readonly WeakReference _weakTarget;
            private readonly MethodInfo _method;
            private readonly OpenEventHandler _openHandler;

            public WeakDelegate(Delegate handler)
            {
                _weakTarget = handler.Target != null ? new WeakReference(handler.Target) : null;
                _method = handler.GetMethodInfo();
                _openHandler = _openHandlerCache.GetOrAdd(_method, CreateOpenHandler);
            }

            public bool Invoke(object sender, TEventArgs e)
            {
                object target = null;
                if (_weakTarget != null)
                {
                    if (!_weakTarget.IsAlive) return false;
                    target = _weakTarget.Target;
                }
                if (SEventProxy.CanFireEvent(e.GetType()))
                {
                    // if we are on UI thread just invoke if not go trough InvokeDelegate 
                    if (!SEventProxy.InvokeOnUIThread(_openHandler, new object[] { target, sender, e }))
                    _openHandler(target, sender, e);
                }                    
                return true;
            }

            public bool IsMatch(object handler)
            {
                return ReferenceEquals((handler as Delegate).Target, _weakTarget?.Target)
                    && (handler as Delegate).GetMethodInfo().Equals(_method);
            }
        }
    }
}
