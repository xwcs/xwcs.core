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
        Action<String> ActionDebug([CallerFilePath] string path="");
		void Debug(string msg,
                    [CallerLineNumber] int line = 0,
                    [CallerFilePath] string path = "",
                    [CallerMemberName] string method = "");
		void Info(string msg,
                    [CallerLineNumber] int line = 0,
                    [CallerFilePath] string path = "",
                    [CallerMemberName] string method = "");
		void Warn(string msg,
                    [CallerLineNumber] int line = 0,
                    [CallerFilePath] string path = "",
                    [CallerMemberName] string method = "");
		void Error(string msg,
                    [CallerLineNumber] int line = 0,
                    [CallerFilePath] string path = "",
                    [CallerMemberName] string method = "");
		void Fatal(string msg,
                    [CallerLineNumber] int line = 0,
                    [CallerFilePath] string path = "",
                    [CallerMemberName] string method = "");
        /*
        void Debug([CallerLineNumber] int line = 0,
                    [CallerFilePath] string path = "",
                    [CallerMemberName] string method = "",
                    string fmt = "", params object[] values);
        void Info([CallerLineNumber] int line = 0,
                    [CallerFilePath] string path = "",
                    [CallerMemberName] string method = "",
                    string fmt = "", params object[] values);
        void Warn([CallerLineNumber] int line = 0,
                    [CallerFilePath] string path = "",
                    [CallerMemberName] string method = "",
                    string fmt = "", params object[] values);
        void Error([CallerLineNumber] int line = 0,
                    [CallerFilePath] string path = "",
                    [CallerMemberName] string method = "",
                    string fmt = "", params object[] values);
        void Fatal([CallerLineNumber] int line = 0,
                    [CallerFilePath] string path = "",
                    [CallerMemberName] string method = "",
                    string fmt = "", params object[] values);
        */
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
        public string Path;
        public int Line;
        public System.DateTime dateTime;
        public int Thread;

    }
    public static class IntervalLogAction
    {
        /*
        public static void Invoke(Action action, ILogger logger,
                                [CallerLineNumber] int line = 0,
                                [CallerFilePath] string path = "",
                                [CallerMemberName] string method = "",
                                string logFmt="", params object[] values)
        {
            logger.Debug($"{SLogManager.OPEN_INTERVAL_LOG}{String.Format(logFmt, values)}", line, path, method);
            try
            {
                action.Invoke();
                
                logger.Debug(SLogManager.CLOSE_INTERVAL_LOG, line, path, method);
            }
            catch (Exception ex)
            {
                logger.Error($"{SLogManager.CLOSE_INTERVAL_LOG}{SLogManager.CLOSE_INTERVAL_LOG}{ex}");
                throw ex;
            }
        }

        public static void Invoke(Action action, ILogger logger, byte warnSecs,
                                [CallerLineNumber] int line = 0,
                                [CallerFilePath] string path = "",
                                [CallerMemberName] string method = "",
                                string logFmt = "", params object[] values)
        {

            logger.Debug($"{SLogManager.OPEN_INTERVAL_LOG}{String.Format(logFmt, values)}", line, path, method);
            try
            {
                action.Invoke();
                logger.Debug(SLogManager.CLOSE_INTERVAL_LOG, line, path, method);
            }
            catch (Exception ex)
            {
                logger.Error($"{SLogManager.CLOSE_INTERVAL_LOG}{SLogManager.CLOSE_INTERVAL_LOG}{ex}");
                throw ex;
            }
        }
        */
        public static void Invoke(Action action, ILogger logger,
                                [CallerLineNumber] int line = 0,
                                [CallerFilePath] string path = "",
                                [CallerMemberName] string method = "",
                                string logMsg="")
        {
            logger.Debug($"{SLogManager.OPEN_INTERVAL_LOG}{logMsg}", line, path, method);
            try
            {
                action.Invoke();

                logger.Debug(SLogManager.CLOSE_INTERVAL_LOG, line, path, method);
            }
            catch (Exception ex)
            {
                logger.Error($"{SLogManager.CLOSE_INTERVAL_LOG}{SLogManager.CLOSE_INTERVAL_LOG}{ex}");
                throw ex;
            }
        }

        public static void Invoke(Action action, ILogger logger, byte warnSecs,
                                [CallerLineNumber] int line = 0,
                                [CallerFilePath] string path = "",
                                [CallerMemberName] string method = "", string logMsg="")
        {

            logger.Debug($"{SLogManager.OPEN_INTERVAL_LOG}{logMsg}", line, path, method);
            try
            {
                action.Invoke();
                logger.Debug(SLogManager.CLOSE_INTERVAL_LOG, line, path, method);
            }
            catch (Exception ex)
            {
                logger.Error($"{SLogManager.CLOSE_INTERVAL_LOG}{SLogManager.CLOSE_INTERVAL_LOG}{ex}");
                throw ex;
            }
        }
    }
    public static class IntervalLogFunction<T>
    {
        /*
        public static T Invoke(Func<T> action, ILogger logger,
                                [CallerLineNumber] int line = 0,
                                [CallerFilePath] string path = "",
                                [CallerMemberName] string method = "", string logFmt="", params object[] values)
        {
            logger.Debug($"{SLogManager.OPEN_INTERVAL_LOG}{String.Format(logFmt, values)}", line, path, method);
            try
            {
                T ret = action.Invoke();
                logger.Debug(SLogManager.CLOSE_INTERVAL_LOG, line, path, method);
                return ret;
            }
            catch (Exception ex)
            {
                logger.Error($"{SLogManager.CLOSE_INTERVAL_LOG}{SLogManager.CLOSE_INTERVAL_LOG}{ex}");
                throw ex;
            }
        }
        public static T Invoke(Func<T> action, ILogger logger, byte warnSecs,
                                [CallerLineNumber] int line = 0,
                                [CallerFilePath] string path = "",
                                [CallerMemberName] string method = "", string logFmt="", params object[] values)
        {
            logger.Debug($"{SLogManager.OPEN_INTERVAL_LOG}{String.Format(logFmt, values)}", line, path, method);
            try
            {
                T ret = action.Invoke();
                logger.Debug(SLogManager.CLOSE_INTERVAL_LOG, line, path, method);
                return ret;
            }
            catch (Exception ex)
            {
                logger.Error($"{SLogManager.CLOSE_INTERVAL_LOG}{SLogManager.CLOSE_INTERVAL_LOG}{ex}");
                throw ex;
            }
        }
        */
        public static T Invoke(Func<T> action, ILogger logger, string logMsg)
        {
            logger.Debug($"{SLogManager.OPEN_INTERVAL_LOG}{logMsg}");
            try
            {
                T ret = action.Invoke();
                logger.Debug(SLogManager.CLOSE_INTERVAL_LOG);
                return ret;
            }
            catch (Exception ex)
            {
                logger.Error($"{SLogManager.CLOSE_INTERVAL_LOG}{SLogManager.CLOSE_INTERVAL_LOG}{ex}");
                throw ex;
            }
        }
        public static T Invoke(Func<T> action, ILogger logger, byte warnSecs, string logMsg)
        {
            logger.Debug($"{SLogManager.OPEN_INTERVAL_LOG}{warnSecs}{SLogManager.OPEN_INTERVAL_LOG}{logMsg}");
            try
            {
                T ret = action.Invoke();
                logger.Debug(SLogManager.CLOSE_INTERVAL_LOG);
                return ret;
            }
            catch (Exception ex)
            {
                logger.Error($"{SLogManager.CLOSE_INTERVAL_LOG}{SLogManager.CLOSE_INTERVAL_LOG}{ex}");
                throw ex;
            }
        }
    }
    [cfg.attr.Config("MainAppConfig")]
    public class SLogManager : cfg.Configurable, ILogger, IDisposable
    {
        public const string OPEN_INTERVAL_LOG = "<<<";
        public const string CLOSE_INTERVAL_LOG = ">>>";

        private bool _fastClose = false;
        private bool _intervalLog = false;
        private static SLogManager instance;
		private ILogger global = null;

        private Dictionary<string, ILogger> _loggers = new Dictionary<string, ILogger>();

        private class LoggerIntervalDecorator : ILogger
        {
            private class LoggerIntervalDecoratorStackElement : Tuple<long, long, string, object[], int>
            {
                public LoggerIntervalDecoratorStackElement(long startat, long elapsingalarm, string fmt, object[] values, int firstmsgindex) : base(startat, elapsingalarm, fmt, values, firstmsgindex)
                {
                }

                public long StartAt { get { return this.Item1; } }
                public long ElapsingAlarm { get { return this.Item2; } }
                public string Fmt { get { return this.Item3; } }
                public object[] Values { get { return this.Item4; } }
                public int FirstMsgIndex { get { return this.Item5; } }
            }
            private ILogger l;
            private System.Diagnostics.Stopwatch stopwatch;
            private const long ELAPSING_WARNING_DEFAULT = 10000;
            private System.Collections.Concurrent.ConcurrentDictionary<int, Tuple<List<String>, Stack<LoggerIntervalDecoratorStackElement>>> _dic_stackOfInterval;
            public LoggerIntervalDecorator(ILogger l)
            {
                _dic_stackOfInterval = new System.Collections.Concurrent.ConcurrentDictionary<int, Tuple<List<String>, Stack<LoggerIntervalDecoratorStackElement>>>();
                //stackOfInterval = new Stack<LoggerIntervalDecoratorStackElement>();
                this.l = l;

            }
            private bool existsThreadData()
            {
                return _dic_stackOfInterval.ContainsKey(Thread.CurrentThread.ManagedThreadId);
            }
            private Tuple<List<String>, Stack<LoggerIntervalDecoratorStackElement>> threadData
            {
                get
                {
                    if (!existsThreadData())
                    {
                        _dic_stackOfInterval[Thread.CurrentThread.ManagedThreadId] = new Tuple<List<String>, Stack<LoggerIntervalDecoratorStackElement>>(new List<String>(), new Stack<LoggerIntervalDecoratorStackElement>());
                    }
                    return _dic_stackOfInterval[Thread.CurrentThread.ManagedThreadId];
                }
            }

            private List<String> stackLogList {
                get {
                    return threadData.Item1;
                }
            }
            private Stack<LoggerIntervalDecoratorStackElement> stackOfInterval
            {
                get
                {
                    return threadData.Item2;
                }
            }
            private void clearStack()
            {
                foreach(var d in _dic_stackOfInterval)
                {
                    d.Value.Item1.Clear();
                    d.Value.Item2.Clear();
                }
                _dic_stackOfInterval.Clear();
                if (!ReferenceEquals(stopwatch, null))
                {
                    if (stopwatch.IsRunning) stopwatch.Stop();
                    stopwatch = null;
                }

            }
            private void clearThreadStack()
            {
                if (existsThreadData())
                {
                    Tuple<List<String>, Stack<LoggerIntervalDecoratorStackElement>> v;
                    if (_dic_stackOfInterval.TryRemove(Thread.CurrentThread.ManagedThreadId, out v))
                    {
                        v?.Item1.Clear();
                        v?.Item2.Clear();
                    }
                    
                }
                if (_dic_stackOfInterval.Count==0) clearStack();
            }
            public void ClearQueue()
            {
                clearStack();
                l.ClearQueue();
            }

            private void PushEvent(string fmt, params object[] values)
            {
                if (!_beginEvent) return;
                _beginEvent = false;
                if (ReferenceEquals(stopwatch, null)) stopwatch = new System.Diagnostics.Stopwatch();
                if (!stopwatch.IsRunning) stopwatch.Start();
                stackOfInterval.Push(new LoggerIntervalDecoratorStackElement(stopwatch.ElapsedMilliseconds, _ElapsingWarning < 0 ? 0 : _ElapsingWarning, fmt, values, threadData.Item1.Count));
                if (this.CurrentDeep > 1)
                {
                    this.addLogTostackLogList(String.Format(fmt, values));
                }

            }

            private void PushEvent(string msg)
            {
                if (!_beginEvent) return;
                this.PushEvent("{0}", msg);
            }
            private object tryJsonDeserialize(string par)
            {
                if (
                    (par.IndexOf('[') >= 0 && par.IndexOf(']') > 0)
                    ||
                    (par.IndexOf('{') >= 0 && par.IndexOf('}') > 0)
                    ) {
                    try
                    {
                        var o = Newtonsoft.Json.JsonConvert.DeserializeObject(par);
                        return o;
                    }
                    catch
                    {
                        return par;
                    }
                } else
                {
                    return par;
                }

            }
            private void PopEvent(string fmt, params object[] values)
            {
                if (!_endEvent) return;
                _endEvent = false;
                int deep = CurrentDeep;
                if (deep == 0) return;
                long fine;
                if (!ReferenceEquals(stopwatch, null) && stopwatch.IsRunning)
                {
                    fine = stopwatch.ElapsedMilliseconds;
                }
                else
                {
                    fine = -1;
                }
                var e = stackOfInterval.Pop();
                object m;
                var l = this.stackLogList;
                if (l.Count > e.FirstMsgIndex + 1)
                {

                    var ml = new List<object>();
                    for (var i = e.FirstMsgIndex+1; i < l.Count; i++)
                    {
                        String item = l[i];
                        if (item.Length > 0)
                        {
                            ml.Add(tryJsonDeserialize(item));
                        }
                    }
                    string s = String.Format(fmt, values);
                    if (s.Length > 0) {
                        ml.Add(tryJsonDeserialize(s));
                    }
                    if (ml.Count>1) {
                        m = ml.ToArray();
                    } else if (ml.Count==0) {
                        m = "";
                    } else {
                        m = ml[0];
                    }
                    
                }
                else
                {
                    m = tryJsonDeserialize(String.Format(fmt, values));
                }

                if (deep == 1)
                {
                    clearThreadStack();
                }
                long durata;
                if (fine >= 0)
                {
                    durata = fine - e.StartAt;
                }
                else
                {
                    fine = e.StartAt;
                    durata = 0;
                }
                if (deep > 1) {
                    this.addLogTostackLogList(String.Format(e.Fmt, e.Values));
                    this.addLogTostackLogList(String.Format(fmt, values));
                }
                if (durata >= e.ElapsingAlarm)
                {
                    DateTime begin = DateTime.Now.AddMilliseconds(-1 * e.StartAt);
                    DateTime end = DateTime.Now.AddMilliseconds(-1 * fine);
                    var messaggio = Newtonsoft.Json.JsonConvert.SerializeObject(new { slow = new { deep = deep, elapsed = durata, thread = Thread.CurrentThread.ManagedThreadId }, begin = new { datetime = begin, msg = tryJsonDeserialize(String.Format(e.Fmt, e.Values)) }, end = new { datetime = end, msg = m } });
                    this.Warn(messaggio);
                }
            }

            private void PopEvent(string msg)
            {
                if (!_endEvent) return;
                this.PopEvent("{0}", msg);
            }

            private bool _beginEvent = false;
            private long _ElapsingWarning = 0;
            /// <summary>
            /// Indica che il successivo log inviato sarà un messaggio di inizio di un evento di cui misurerò la durata
            /// </summary>
            private void BeginEvent(long millisecElapsingWarning = ELAPSING_WARNING_DEFAULT)
            {
                _beginEvent = true;
                _ElapsingWarning = millisecElapsingWarning;
            }
            private void BeginEvent()
            {
                BeginEvent(ELAPSING_WARNING_DEFAULT);
            }
            private int CurrentDeep {
                get {
                    if (!existsThreadData()) return 0;
                    return stackOfInterval.Count();
                }
            }
            private void ReturnToDeep(int deep)
            {
                while (CurrentDeep > (deep >= 0 ? deep : 0))
                {
                    stackOfInterval.Pop();
                }
                if (CurrentDeep == 0) clearThreadStack();
            }
            private bool _endEvent = false;
            /// <summary>
            /// Indica che il successivo log inviato sarà un messaggio di fine di un evento di cui ho misurato la durata
            /// </summary>
            private void EndEvent()
            {
                _endEvent = true;
            }
            private string EndMessage(string msg)
            {
                if (msg.StartsWith(SLogManager.CLOSE_INTERVAL_LOG))
                {
                    EndEvent();
                    return msg.Substring(3);
                }
                return msg;
            }
            private string BeginMessage(string msg)
            {
                if (msg.StartsWith(OPEN_INTERVAL_LOG))
                {
                    var m = System.Text.RegularExpressions.Regex.Matches(msg, $"^{OPEN_INTERVAL_LOG}(\\d+){OPEN_INTERVAL_LOG}");
                    if (m.Count > 0)
                    {
                        BeginEvent(int.Parse(m[0].Captures[0].Value.Replace(OPEN_INTERVAL_LOG,""))*1000);
                        return msg.Substring(m[0].Length);
                    }
                    else
                    {
                        BeginEvent();
                        return msg.Substring(3);
                    }
                }
                return msg;
            }
            private string autoStartStop(string msg)
            {
                string m = BeginMessage(msg);
                if (m.Equals(msg)) m = EndMessage(msg);
                return m;
            }

            private void addLogTostackLogList(string msg)
            {
                if (existsThreadData())
                {
                    if (!(ReferenceEquals(msg, null) || msg.Length == 0))
                    {
                        this.threadData.Item1.Add(msg);
                    }
                }

            }
            private string preWorkMsg(string msg)
            {
                string m = autoStartStop(msg);
                if (_beginEvent) PushEvent(m);
                if (_endEvent) PopEvent(m);
                return msg;
            }
            private string preWorkMsg(string fmt, params object[] values)
            {
                string m = autoStartStop(fmt);
                if (_beginEvent) PushEvent(m, values);
                if (_endEvent) PopEvent(m, values);
                
                return fmt;
            }

            public Action<string> ActionDebug([CallerFilePath] string path = "")
            {
                return (msg) => this.Debug(msg: msg, path: path, method: "", line: 0);
            }

            public void Debug(string msg,
                [CallerLineNumber] int line = 0,
                [CallerFilePath] string path = "",
                [CallerMemberName] string method = "")
            {
                l?.Debug(preWorkMsg(msg), line, path, method);
            }
            /*
            public void Debug([CallerLineNumber] int line = 0,
                    [CallerFilePath] string path = "",
                    [CallerMemberName] string method = "",
                    string fmt = "", params object[] values)
            {
                l?.Debug(line, path, method, preWorkMsg(fmt, values), values);
            }
            */
            public void Dispose()
            {
                clearStack();
                l?.Dispose();
                l = null;
                
            }

            public void Error(string msg,
                [CallerLineNumber] int line = 0,
                [CallerFilePath] string path = "",
                [CallerMemberName] string method = "")
            {
                l?.Error(preWorkMsg(msg), line, path, method);
            }
            /*
            public void Error([CallerLineNumber] int line = 0,
                    [CallerFilePath] string path = "",
                    [CallerMemberName] string method = "",
                    string fmt = "", params object[] values)
            {
                l?.Error(line, path, method, preWorkMsg(fmt, values), values);
            }
            */
            public void Fatal(string msg,
                [CallerLineNumber] int line = 0,
                [CallerFilePath] string path = "",
                [CallerMemberName] string method = "")
            {
                l?.Fatal(preWorkMsg(msg), line, path, method);
            }
            /*
            public void Fatal([CallerLineNumber] int line = 0,
                    [CallerFilePath] string path = "",
                    [CallerMemberName] string method = "",
                    string fmt = "", params object[] values)
            {
                l?.Fatal(line, path, method, preWorkMsg(fmt, values), values);
            }
            */
            public void Info(string msg,
                [CallerLineNumber] int line = 0,
                [CallerFilePath] string path = "",
                [CallerMemberName] string method = "")
            {
                l?.Info(preWorkMsg(msg), line, path, method);
            }
            /*
            public void Info([CallerLineNumber] int line = 0,
                    [CallerFilePath] string path = "",
                    [CallerMemberName] string method = "",
                    string fmt = "", params object[] values)
            {
                l?.Info(line, path, method, preWorkMsg(fmt, values), values);
            }
            */
            public void Warn(string msg,
                [CallerLineNumber] int line = 0,
                [CallerFilePath] string path = "",
                [CallerMemberName] string method = "")
            {
                l?.Warn(preWorkMsg(msg),line,path,method);
            }
            /*
            public void Warn([CallerLineNumber] int line = 0,
                    [CallerFilePath] string path = "",
                    [CallerMemberName] string method = "",
                    string fmt = "", params object[] values)
            {
                l?.Warn(line, path, method, preWorkMsg(fmt, values), values);
            }
            */
        }

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
                    LogMessage t = new LogMessage() { Kind = LogKind.N, Message = "", Method = "", Line = 0, dateTime = DateTime.Now, Path = "", Thread = 0};

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
                            StringBuilder sb = new StringBuilder();
                            sb.Append(String.Concat("\"D\":\"", t.dateTime.ToString("yyyy-MM-dd HH:mm:ss,fff"), "\""));
                            sb.Append($",\"T\":{t.Thread}");
                            string lev;
                            switch (t.Kind) {
                                case LogKind.D:
                                    lev = "DEBUG";
                                    break;
                                case LogKind.E:
                                    lev = "ERROR";
                                    break;
                                case LogKind.F:
                                    lev = "FATAL";
                                    break;
                                case LogKind.I:
                                    lev = "INFO";
                                    break;
                                case LogKind.W:
                                    lev = "WARN";
                                    break;
                                default:
                                    lev = t.Kind.ToString();
                                    break;
                            }
                            sb.Append($",\"L\":\"{lev}\"");
                            sb.Append($",\"G\":\"{logger.Logger.Name}\"");
                            
                            //sb.Append(t.Message.Substring(1,t.Message.Length-2));
                            sb.Append(",");
                            try
                            {
                                //SE LA DESERIALIZZAZIONE JSON FUNZIONA ALLORA IL MESSAGGIO è GIà UN JSON VALIDONewtonsoft.Json.JsonConvert.DeserializeObject(t.Message);
                                var j = Newtonsoft.Json.JsonConvert.DeserializeObject(t.Message);
                                if (ReferenceEquals(j, null))
                                {
                                    var d = new Dictionary<String, String>();
                                    d.Add("M", t.Message);
                                    string s = Newtonsoft.Json.JsonConvert.SerializeObject(d, Newtonsoft.Json.Formatting.None);
                                    sb.Append(s.Substring(1, s.Length - 2));
                                }
                                else
                                {
                                    sb.Append("\"M\": " + t.Message + "");
                                }
                            }
                            catch
                            {
                                var d = new Dictionary<String, String>();
                                d.Add("M", t.Message);
                                string s = Newtonsoft.Json.JsonConvert.SerializeObject(d, Newtonsoft.Json.Formatting.None);
                                sb.Append(s.Substring(1,s.Length-2));
                            }
                            if (!ReferenceEquals(t.Method, null) && !t.Method.Equals(""))
                            {
                                sb.Append(",\"F\":");
                                sb.Append(Newtonsoft.Json.JsonConvert.SerializeObject(t.Method));
                            }
                            if (!ReferenceEquals(t.Path, null) && !t.Path.Equals(""))
                            {
                                sb.Append(",\"P\":");
                                sb.Append(Newtonsoft.Json.JsonConvert.SerializeObject(t.Path.Substring(_RootProjectPathLength)));
                            }
                            if (t.Line > 0)
                            {
                                sb.Append($",\"R\":{t.Line}");
                            }
                            string m = sb.ToString();
                            switch (t.Kind)
                            {
                                case LogKind.E:
                                case LogKind.F:
                                    t.Message = $"{t.Message} in : {t.Method}({t.Line})";
                                    break;
                                default:
                                    break;
                            }
                            switch (t.Kind)
                            {
                                case LogKind.D:
                                    _proxy.fireEvent(new OutputMessageEvent(this, new OutputMessage { Message = string.Format("[{2}]{0} - {1}", logger.Logger.Name, t.Message, t.Kind.ToString()) }));
                                    logger.Debug(m);
                                    break;
                                case LogKind.E:
                                    _proxy.fireEvent(new OutputMessageEvent(this, new OutputMessage { Message = string.Format("[{3}]{0} - {1} - {2}({4})", logger.Logger.Name, t.Message, t.Method, t.Kind.ToString(), t.Line) }));
                                    logger.Error(m);
                                    break;
                                case LogKind.F:
                                    _proxy.fireEvent(new OutputMessageEvent(this, new OutputMessage { Message = string.Format("[{3}]{0} - {1} - {2}({4})", logger.Logger.Name, t.Message, t.Method, t.Kind.ToString(), t.Line) }));
                                    logger.Fatal(m);
                                    break;
                                case LogKind.I:
                                    _proxy.fireEvent(new OutputMessageEvent(this, new OutputMessage { Message = string.Format("[{2}]{0} - {1}", logger.Logger.Name, t.Message, t.Kind.ToString()) }));
                                    logger.Info(m);
                                    break;
                                case LogKind.W:
                                    _proxy.fireEvent(new OutputMessageEvent(this, new OutputMessage { Message = string.Format("[{2}]{0} - {1}", logger.Logger.Name, t.Message, t.Kind.ToString()) }));
                                    logger.Warn(m);
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

            public Action<string> ActionDebug([CallerFilePath] string path = "")
            {
                return (msg) => this.Debug(msg: msg, path: path, method: "", line: 0);
            }

            public void Debug(string msg,
                            [CallerLineNumber] int line = 0,
                            [CallerFilePath] string path = "",
                            [CallerMemberName] string method = "")
			{
				if (!logger.IsDebugEnabled) return;
                lock (((ICollection)_queue).SyncRoot)
                {
                    _queue.Enqueue(new LogMessage() { Kind= LogKind.D, Message= msg, dateTime = DateTime.Now, Line=line, Method = method, Path = path, Thread = Thread.CurrentThread.ManagedThreadId});
                }
                _syncEvents.NewTransitionEvent.Set();
            }

			public void Info(string msg,
                            [CallerLineNumber] int line = 0,
                            [CallerFilePath] string path = "",
                            [CallerMemberName] string method = "")
			{
                if (!logger.IsInfoEnabled) return;
                lock (((ICollection)_queue).SyncRoot)
                {
                    _queue.Enqueue(new LogMessage() { Kind = LogKind.I, Message = msg, dateTime = DateTime.Now, Line = line, Path = path, Method = method, Thread = Thread.CurrentThread.ManagedThreadId });
                }
                _syncEvents.NewTransitionEvent.Set();

            }

            public void Warn(string msg,
                            [CallerLineNumber] int line = 0,
                            [CallerFilePath] string path = "",
                            [CallerMemberName] string method = "")
			{
                if (!logger.IsWarnEnabled) return;
                lock (((ICollection)_queue).SyncRoot)
                {
                    _queue.Enqueue(new LogMessage() { Kind = LogKind.W, Message = msg, dateTime = DateTime.Now, Line = line, Path = path, Method = method, Thread = Thread.CurrentThread.ManagedThreadId });
                }
                _syncEvents.NewTransitionEvent.Set();

            }

            public void Error(string msg,
                            [CallerLineNumber] int line = 0,
                            [CallerFilePath] string path = "",
                            [CallerMemberName] string method = "")
			{
				if (!logger.IsErrorEnabled) return;

                StackFrame sf = new StackFrame(1);
                lock (((ICollection)_queue).SyncRoot)
                {
                    _queue.Enqueue(new LogMessage() { Kind = LogKind.E, Message = msg, Method = sf.GetMethod().Name, Line = sf.GetFileLineNumber(), dateTime = DateTime.Now, Path = sf.GetFileName(), Thread = Thread.CurrentThread.ManagedThreadId });
                }
                _syncEvents.NewTransitionEvent.Set();

            }

            public void Fatal(string msg,
                            [CallerLineNumber] int line = 0,
                            [CallerFilePath] string path = "",
                            [CallerMemberName] string method = "")
			{
				if (!logger.IsFatalEnabled) return;

                StackFrame sf = new StackFrame(1);

                lock (((ICollection)_queue).SyncRoot)
                {
                    _queue.Enqueue(new LogMessage() { Kind = LogKind.F, Message = msg, Method = sf.GetMethod().Name, Line = sf.GetFileLineNumber(), dateTime = DateTime.Now, Path = sf.GetFileName(), Thread = Thread.CurrentThread.ManagedThreadId });
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
            /*
            public void Debug([CallerLineNumber] int line = 0,
                    [CallerFilePath] string path = "",
                    [CallerMemberName] string method = "",
                    string fmt = "", params object[] values)
            {
                if (!logger.IsDebugEnabled) return;
                Debug(string.Format(fmt, values), line, path, method);
            }

            public void Info([CallerLineNumber] int line = 0,
                    [CallerFilePath] string path = "",
                    [CallerMemberName] string method = "",
                    string fmt = "", params object[] values)
            {
                if (!logger.IsInfoEnabled) return;
                Info(string.Format(fmt, values), line, path, method);
            }

            public void Warn([CallerLineNumber] int line = 0,
                    [CallerFilePath] string path = "",
                    [CallerMemberName] string method = "",
                    string fmt = "", params object[] values)
            {
                if (!logger.IsWarnEnabled) return;
                Warn(string.Format(fmt, values), line, path, method);
            }

            public void Error([CallerLineNumber] int line = 0,
                    [CallerFilePath] string path = "",
                    [CallerMemberName] string method = "",
                    string fmt = "", params object[] values)
            {
                if (!logger.IsErrorEnabled) return;
                Error(string.Format(fmt, values), line, path, method);
            }

            public void Fatal([CallerLineNumber] int line = 0,
                    [CallerFilePath] string path = "",
                    [CallerMemberName] string method = "",
                    string fmt = "", params object[] values)
            {
                if (!logger.IsFatalEnabled) return;
                Fatal(msg:string.Format(fmt, values),line:line,path:path,method:method);
            }
            */
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
            _RootProjectPathLength = RootProjectPath().Length;
            _fastClose = getCfgParam("SLogManager/FastClose", "No") == "Yes";
            _intervalLog = getCfgParam("SLogManager/IntervalLog", "No") == "Yes";
        }
        private static int _RootProjectPathLength=0;
        private static string RootProjectPath([CallerFilePath] string path = "")
        {
            return path.Substring(0, path.LastIndexOf("xwcs.core\\"));
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
                
                if (_intervalLog)
                {
                    //per disabilitare le operazioni relative alla issue 290 togliere LoggerIntervalDecorator da qui
                    _loggers[t.ToString()] = new LoggerIntervalDecorator(new SimpleLogger(t.ToString()));
                } else
                {
                    _loggers[t.ToString()] = new SimpleLogger(t.ToString());
                }
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
            return ex.ToString();
        }

        /****

            MAIN methods
        */
        public Action<string> ActionDebug([CallerFilePath] string path = "")
        {
            return (msg) => this.Debug(msg: msg, path: path, method:"", line:0);
        }

        public void Debug(string msg,
                        [CallerLineNumber] int line = 0,
                        [CallerFilePath] string path = "",
                        [CallerMemberName] string method = "")
		{
			global.Debug(msg: msg, line: line,path: path,method: method);
		}

		public void Info(string msg,
                        [CallerLineNumber] int line = 0,
                        [CallerFilePath] string path = "",
                        [CallerMemberName] string method = "")
		{
			global.Info(msg, line: line, path: path, method: method);
        }

		public void Warn(string msg,
                        [CallerLineNumber] int line = 0,
                        [CallerFilePath] string path = "",
                        [CallerMemberName] string method = "")
		{
			global.Warn(msg, line: line, path: path, method: method);
        }

		public void Error(string msg,
                        [CallerLineNumber] int line = 0,
                        [CallerFilePath] string path = "",
                        [CallerMemberName] string method = "")
		{
			global.Error(msg, line: line, path: path, method: method);
        }

		public void Fatal(string msg,
                        [CallerLineNumber] int line = 0,
                        [CallerFilePath] string path = "",
                        [CallerMemberName] string method = "")
		{
			global.Fatal(msg, line: line, path: path, method: method);
        }
        /*
        public void Debug([CallerLineNumber] int line = 0,
                    [CallerFilePath] string path = "",
                    [CallerMemberName] string method = "",
                    string fmt = "", params object[] values)
        {
            global.Debug(line, path, method, fmt, values);
        }
        */
        /*
        public void Info([CallerLineNumber] int line = 0,
                    [CallerFilePath] string path = "",
                    [CallerMemberName] string method = "",
                    string fmt = "", params object[] values)
        {
            global.Info(line, path, method, fmt, values);
        }
        */
        /*
        public void Warn([CallerLineNumber] int line = 0,
                    [CallerFilePath] string path = "",
                    [CallerMemberName] string method = "",
                    string fmt = "", params object[] values)
        {
            global.Warn(line, path, method, fmt, values);
        }
        */
        /*
        public void Error([CallerLineNumber] int line = 0,
                    [CallerFilePath] string path = "",
                    [CallerMemberName] string method = "",
                    string fmt = "", params object[] values)
        {
            global.Error(line, path, method, fmt, values);
        }
        */
        /*
        public void Fatal([CallerLineNumber] int line = 0,
                    [CallerFilePath] string path = "",
                    [CallerMemberName] string method = "",
                    string fmt = "", params object[] values)
        {
            global.Fatal(line, path, method, fmt, values);
        }
        */
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
