using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xwcs.core.plgs;
using xwcs.core.evt;
using System.Runtime.CompilerServices;
using log4net;


[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace xwcs.core.manager
{
	public interface ILogger {
		void Debug(string msg);
		void Info(string msg);
		void Warn(string msg);
		void Error(string msg);
		void Fatal(string msg);
	}
	
    public class SLogManager : ILogger
    {
        private static SLogManager instance;
		private ILogger global = null;

        private Dictionary<string, ILogger> _loggers = new Dictionary<string, ILogger>();

        private class SimpleLogger : ILogger
		{

			private static SEventProxy _proxy;
            private ILog logger = null;

			public SimpleLogger() : this("Global")
			{
			}

			public SimpleLogger(string name)
			{
				_proxy = SEventProxy.getInstance();
				logger = LogManager.GetLogger(name);
			}

			public SimpleLogger(Type t) : this(t.Name) { }

			public void Debug(string msg)
			{
				if (!logger.IsDebugEnabled) return;
				_proxy.fireEvent(new OutputMessageEvent(this, new OutputMessage { Message = string.Format("{0} - {1}", logger.Logger.Name, msg) }));
				logger.Debug(msg);
			}

			public void Info(string msg)
			{
				if (!logger.IsInfoEnabled) return;
				_proxy.fireEvent(new OutputMessageEvent(this, new OutputMessage { Message = string.Format("{0} - {1}", logger.Logger.Name, msg) }));
				logger.Info(msg);
			}

			public void Warn(string msg)
			{
				if (!logger.IsWarnEnabled) return;
				_proxy.fireEvent(new OutputMessageEvent(this, new OutputMessage { Message = string.Format("{0} - {1}", logger.Logger.Name, msg) }));
				logger.Warn(msg);
			}

			public void Error(string msg)
			{
				if (!logger.IsErrorEnabled) return;
				_proxy.fireEvent(new OutputMessageEvent(this, new OutputMessage { Message = string.Format("{0} - {1}", logger.Logger.Name, msg) }));
				logger.Error(msg);
			}

			public void Fatal(string msg)
			{
				if (!logger.IsFatalEnabled) return;
				_proxy.fireEvent(new OutputMessageEvent(this, new OutputMessage { Message = string.Format("{0} - {1}", logger.Logger.Name, msg) }));
				logger.Fatal(msg);
			}
		}


		//singleton need private ctor
		private SLogManager()
        {
			global = new SimpleLogger();
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
	}
}
