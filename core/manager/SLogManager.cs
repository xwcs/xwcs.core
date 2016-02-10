using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xwcs.core.plgs;
using xwcs.core.evt;
using System.Runtime.CompilerServices;

namespace xwcs.core.manager
{
    public class SLogManager
    {
        private static SLogManager instance;
        private static SEventProxy _proxy;

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

        public void log(string msg)
        {
            _proxy.fireEvent(new OutputMessageEvent(this, new OutputMessage { Message = msg }));
            Console.WriteLine(msg);
        }
    }
}
