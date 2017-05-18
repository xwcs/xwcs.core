using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace xwcs.core
{
    public class CmdQueue : IDisposable
    {
        public delegate void VoidNoParamDelegate();


        private System.Windows.Forms.Timer _executeLaterTimer;
        private Queue<VoidNoParamDelegate> _executeLaterQueue;

        public CmdQueue()
        {
            _executeLaterQueue = new Queue<VoidNoParamDelegate>();
            _executeLaterTimer = new Timer();
            _executeLaterTimer.Interval = 1;
            _executeLaterTimer.Enabled = false;
            _executeLaterTimer.Tick += _executeLaterTimer_Tick;
        }

        private void _executeLaterTimer_Tick(object sender, EventArgs e)
        {
            _executeLaterTimer.Stop();
            ConsumeQueue();
        }
        
        public void ExecuteLater(VoidNoParamDelegate d)
        {
            if (disposedValue)
            { // Silent
                throw new InvalidOperationException("State Machine Disposed");
            }
            _executeLaterQueue.Enqueue(d);
            _executeLaterTimer.Start();
            Application.DoEvents();
        }

        
        private void ConsumeQueue()
        {
            if (_executeLaterQueue.Count > 0)
            {
                VoidNoParamDelegate tt = _executeLaterQueue.Dequeue();
                tt();
            }
            if (_executeLaterQueue.Count > 0)
            {
                _executeLaterTimer.Start();
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // Per rilevare chiamate ridondanti

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _executeLaterTimer.Tick -= _executeLaterTimer_Tick;
                    _executeLaterTimer.Dispose();
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
