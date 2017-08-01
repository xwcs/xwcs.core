using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xwcs.core.plgs;
using xwcs.core.evt;
using System.Runtime.CompilerServices;
using log4net;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Collections;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace xwcs.core.manager
{
    #region SyncClasses
    /// <summary>
    /// Synchronization Class.
    /// </summary>
    public class SyncEvents
    {
        public SyncEvents()
        {

            _newTransitionEvent = new AutoResetEvent(false);
            _exitThreadEvent = new ManualResetEvent(false);
            _eventArray = new WaitHandle[2];
            _eventArray[0] = _newTransitionEvent;
            _eventArray[1] = _exitThreadEvent;
        }

        public EventWaitHandle ExitThreadEvent
        {
            get { return _exitThreadEvent; }
        }
        public EventWaitHandle NewTransitionEvent
        {
            get { return _newTransitionEvent; }
        }
        public WaitHandle[] EventArray
        {
            get { return _eventArray; }
        }

        private EventWaitHandle _newTransitionEvent;
        private EventWaitHandle _exitThreadEvent;
        private WaitHandle[] _eventArray;
    }
    #endregion


    public interface ILogger : IDisposable
    {
		void Debug(string msg);
		void Info(string msg);
		void Warn(string msg);
		void Error(string msg);
		void Fatal(string msg);
        void Debug(string fmt, params object[] values);
        void Info(string fmt, params object[] values);
        void Warn(string fmt, params object[] values);
        void Error(string fmt, params object[] values);
        void Fatal(string fmt, params object[] values);
        void ClearQueue();

    }

    public enum LogKind
    {
        N, // no log
        D,
        I,
        W,
        E,
        F
    } 

    public struct LogMessage
    {
        public LogKind Kind;
        public string Message;
        public string Method;
        public int Line;
    }
	
    [cfg.attr.Config("MainAppConfig")]
    public class SLogManager : cfg.Configurable, ILogger, IDisposable
    {

        private bool _fastClose = false;
        private static SLogManager instance;
		private ILogger global = null;

        private Dictionary<string, ILogger> _loggers = new Dictionary<string, ILogger>();



        private class SimpleLogger : ILogger
		{
            
			private static SEventProxy _proxy;
            private ILog logger = null;

            private Queue<LogMessage> _queue = null;
            private SyncEvents _syncEvents = null;
            private Thread _consumerThread = null;

            public SimpleLogger() : this("Global")
			{
                
            }

            

            public void ClearQueue()
            {
                if (System.Threading.Monitor.TryEnter(((ICollection)_queue).SyncRoot, 5000))
                {
                    try
                    {
                        if (_queue.Count > 0)
                        {
                            _queue.Clear();
                        }
                    }
                    finally
                    {
                        System.Threading.Monitor.Exit(((ICollection)_queue).SyncRoot);
                    }
                }
            }

            /// <summary>
            /// Internal Transition Thread.
            /// </summary>
            private void ConsumerThread()
            {
                while (WaitHandle.WaitAny(_syncEvents.EventArray) != 1 && !disposedValue)
                {
                    bool GoWait = false;
                    LogMessage t = new LogMessage() { Kind = LogKind.N, Message = "", Method = "", Line = 0 };

                    while (!GoWait && !disposedValue)
                    {
                        // in lock just work with queue

                        int cnt = 10;
                        bool done = false;

                        do
                        {
                            if (System.Threading.Monitor.TryEnter(((ICollection)_queue).SyncRoot, 5000))
                            {
                                try
                                {
                                    if (_queue.Count > 0)
                                    {
                                        t = _queue.Dequeue();
                                    }
                                    else
                                    {
                                        GoWait = true;
                                    }
                                    done = true;
                                }
                                finally
                                {
                                    System.Threading.Monitor.Exit(((ICollection)_queue).SyncRoot);
                                }
                            }

                        } while (cnt-- > 0 && !done);

                        if (cnt <= 0) throw new ApplicationException("Cant lock logger queue!");
                        
                        // now log if there is something
                        if(!GoWait && !disposedValue)
                        {
                            
                            switch (t.Kind)
                            {
                                case LogKind.D:
                                    _proxy.fireEvent(new OutputMessageEvent(this, new OutputMessage { Message = string.Format("[{2}]{0} - {1}", logger.Logger.Name, t.Message, t.Kind.ToString()) }));
                                    logger.Debug(string.Format("{0}", t.Message));
                                    break;
                                case LogKind.E:
                                    _proxy.fireEvent(new OutputMessageEvent(this, new OutputMessage { Message = string.Format("[{3}]{0} - {1} - {2}({4})", logger.Logger.Name, t.Message, t.Method, t.Kind.ToString(), t.Line) }));
                                    logger.Error(string.Format("{0} in : {1}({2})", t.Message, t.Method, t.Line));
                                    break;
                                case LogKind.F:
                                    _proxy.fireEvent(new OutputMessageEvent(this, new OutputMessage { Message = string.Format("[{3}]{0} - {1} - {2}({4})", logger.Logger.Name, t.Message, t.Method, t.Kind.ToString(), t.Line) }));
                                    logger.Fatal(string.Format("{0} in : {1}({2})", t.Message, t.Method, t.Line));
                                    break;
                                case LogKind.I:
                                    _proxy.fireEvent(new OutputMessageEvent(this, new OutputMessage { Message = string.Format("[{2}]{0} - {1}", logger.Logger.Name, t.Message, t.Kind.ToString()) }));
                                    logger.Info(string.Format("{0}", t.Message));
                                    break;
                                case LogKind.W:
                                    _proxy.fireEvent(new OutputMessageEvent(this, new OutputMessage { Message = string.Format("[{2}]{0} - {1}", logger.Logger.Name, t.Message, t.Kind.ToString()) }));
                                    logger.Warn(string.Format("{0}", t.Message));
                                    break;
                            }
                        }
                    }
                }
            }

            public SimpleLogger(string name)
			{


				_proxy = SEventProxy.getInstance();
				logger = LogManager.GetLogger(name);

                _queue = new Queue<LogMessage>();
                _syncEvents = new SyncEvents();
                _consumerThread = new Thread(ConsumerThread);
                _consumerThread.Start();

            }

			public SimpleLogger(Type t) : this(t.Name) {

                
            }

			public void Debug(string msg)
			{
				if (!logger.IsDebugEnabled) return;
                lock (((ICollection)_queue).SyncRoot)
                {
                    _queue.Enqueue(new LogMessage() { Kind= LogKind.D, Message= msg});
                }
                _syncEvents.NewTransitionEvent.Set();
            }

			public void Info(string msg)
			{
                if (!logger.IsInfoEnabled) return;
                lock (((ICollection)_queue).SyncRoot)
                {
                    _queue.Enqueue(new LogMessage() { Kind = LogKind.I, Message = msg });
                }
                _syncEvents.NewTransitionEvent.Set();

            }

            public void Warn(string msg)
			{
                if (!logger.IsWarnEnabled) return;
                lock (((ICollection)_queue).SyncRoot)
                {
                    _queue.Enqueue(new LogMessage() { Kind = LogKind.W, Message = msg });
                }
                _syncEvents.NewTransitionEvent.Set();

            }

            public void Error(string msg)
			{
				if (!logger.IsErrorEnabled) return;

                StackFrame sf = new StackFrame(1);

                lock (((ICollection)_queue).SyncRoot)
                {
                    _queue.Enqueue(new LogMessage() { Kind = LogKind.E, Message = msg, Method = sf.GetMethod().Name, Line = sf.GetFileLineNumber() });
                }
                _syncEvents.NewTransitionEvent.Set();

            }

            public void Fatal(string msg)
			{
				if (!logger.IsFatalEnabled) return;

                StackFrame sf = new StackFrame(1);

                lock (((ICollection)_queue).SyncRoot)
                {
                    _queue.Enqueue(new LogMessage() { Kind = LogKind.F, Message = msg, Method = sf.GetMethod().Name, Line = sf.GetFileLineNumber() });
                }
                _syncEvents.NewTransitionEvent.Set();
            }

            #region IDisposable Support
            private bool disposedValue = false; // Per rilevare chiamate ridondanti

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // managed things here
                    }
                    //thread must be killed any way
                    _syncEvents.ExitThreadEvent.Set();
                    _consumerThread.Join();

                    disposedValue = true;
                }
            }

            // Questo codice viene aggiunto per implementare in modo corretto il criterio Disposable.
            public void Dispose()
            {
                Dispose(true);
            }

            public void Debug(string fmt, params object[] values)
            {
                if (!logger.IsDebugEnabled) return;
                Debug(string.Format(fmt, values));
            }

            public void Info(string fmt, params object[] values)
            {
                if (!logger.IsInfoEnabled) return;
                Info(string.Format(fmt, values));
            }

            public void Warn(string fmt, params object[] values)
            {
                if (!logger.IsWarnEnabled) return;
                Warn(string.Format(fmt, values));
            }

            public void Error(string fmt, params object[] values)
            {
                if (!logger.IsErrorEnabled) return;
                Error(string.Format(fmt, values));
            }

            public void Fatal(string fmt, params object[] values)
            {
                if (!logger.IsFatalEnabled) return;
                Fatal(string.Format(fmt, values));
            }

            ~SimpleLogger()
            {
                Dispose(false);
            }
            #endregion
        }


		//singleton need private ctor
		private SLogManager()
        {
			global = new SimpleLogger();
            _fastClose = getCfgParam("SLogManager/FastClose", "No") == "Yes";
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static SLogManager getInstance()
        {
            if (instance == null)
            {
                instance = new SLogManager();
            }
            return instance;
        }

		public ILogger getClassLogger(Type t) {
            if (!_loggers.ContainsKey(t.ToString()))
            {
                _loggers[t.ToString()] = new SimpleLogger(t.ToString());
            }
            return _loggers[t.ToString()];

        }

        // stack trace dump
        public static string DumpCallStack(int from, int count, string separator = "->")
        {
            StringBuilder sb = new StringBuilder();
            // skip local call
            ++from;
            string sep = "";
            for (int i = from + count - 1; i >= from; --i)
            {
                MethodBase info = new StackFrame(i).GetMethod();
                sb.Append(string.Format("{0}{1}.{2}", sep, info.DeclaringType?.Name, info.Name));
                sep = separator;
            }
            return sb.ToString();
        }

        public static string GetExceptionString(Exception ex)
        {
            // unwind exception
            while (ex.InnerException != null) ex = ex.InnerException;
            return ex.Message;
        }

        /****

            MAIN methods
        */
        public void Debug(string msg)
		{
			global.Debug(msg);
		}

		public void Info(string msg)
		{
			global.Info(msg);
		}

		public void Warn(string msg)
		{
			global.Warn(msg);
		}

		public void Error(string msg)
		{
			global.Error(msg);
		}

		public void Fatal(string msg)
		{
			global.Fatal(msg);
		}
        public void Debug(string fmt, params object[] values)
        {
            global.Debug(fmt, values);
        }

        public void Info(string fmt, params object[] values)
        {
            global.Info(fmt, values);
        }

        public void Warn(string fmt, params object[] values)
        {
            global.Warn(fmt, values);
        }

        public void Error(string fmt, params object[] values)
        {
            global.Error(fmt, values);
        }

        public void Fatal(string fmt, params object[] values)
        {
            global.Fatal(fmt, values);
        }

        public void ClearQueue()
        {
            global.ClearQueue();
        }


        #region IDisposable Support
        private bool disposedValue = false; // Per rilevare chiamate ridondanti

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    //managed here
                }

                if (_fastClose)
                {
                    global.ClearQueue();
                    //clear queues
                    foreach (ILogger l in _loggers.Values)
                    {
                        l.ClearQueue();
                    }
                }
                foreach (ILogger l in _loggers.Values)
                {
                    l.Dispose();
                }
                global.Dispose();

                disposedValue = true;
            }
        }

        ~SLogManager() {
           Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

       


        #endregion
    }
}
