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
    public class SLogManager
    {
        private static SLogManager instance;
        private static SEventProxy _proxy;

        private ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //singleton need private ctor
        private SLogManager()
        {
            _proxy = SEventProxy.getInstance();
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


        /****

            MAIN methods
        */

        public void Debug(string msg)
        {
            _proxy.fireEvent(new OutputMessageEvent(this, new OutputMessage { Message = msg }));
            Console.WriteLine("D:" + msg);
            logger.Debug(msg);
        }

        public void Info(string msg)
        {
            _proxy.fireEvent(new OutputMessageEvent(this, new OutputMessage { Message = msg }));
            Console.WriteLine("I:" + msg);
            logger.Info(msg);
        }

        public void Warn(string msg)
        {
            _proxy.fireEvent(new OutputMessageEvent(this, new OutputMessage { Message = msg }));
            Console.WriteLine("W:" + msg);
            logger.Warn(msg);
        }

        public void Error(string msg)
        {
            _proxy.fireEvent(new OutputMessageEvent(this, new OutputMessage { Message = msg }));
            Console.WriteLine("E:" + msg);
            logger.Error(msg);
        }

        public void Fatal(string msg)
        {
            _proxy.fireEvent(new OutputMessageEvent(this, new OutputMessage { Message = msg }));
            Console.WriteLine("F:" + msg);
            logger.Fatal(msg);
        }
    }
}
