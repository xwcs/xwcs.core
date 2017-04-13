using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xwcs.core.plgs;
using xwcs.core.evt;
using System.Runtime.CompilerServices;
using System.ComponentModel;

namespace xwcs.core.statemachine
{	
    public class SStateMachineCtx
    {
        private static SStateMachineCtx instance;

        private ISynchronizeInvoke _invokeDelegate = null;

        //singleton need private ctor
        private SStateMachineCtx()
        {
			
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static SStateMachineCtx getInstance()
        {
            if (instance == null)
            {
                instance = new SStateMachineCtx();
            }
            return instance;
        }


        public ISynchronizeInvoke InvokeDelegate
        {
            get {
                return _invokeDelegate;    
            }
            set
            {
                _invokeDelegate = value;
            }
        }

        public bool Invoke(Delegate what, object[] args)
        {
            if(_invokeDelegate != null)
            {
                if (_invokeDelegate.InvokeRequired)
                {
                    _invokeDelegate.Invoke(what, args);
                    return true;
                }

                return false;
            }

            throw new ApplicationException("Missing Invocation delegate!");
        } 

    }
}
