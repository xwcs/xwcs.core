using DevExpress.XtraBars;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace xwcs.core.statemachine
{

    public class StateMachineUiTrigger
    {
        public BarItem Item { get; private set; }
        public string TriggerName { get; private set; } 

        public StateMachineUiTrigger(BarItem bi, string n)
        {
            Item = bi;
            TriggerName = n;
        }
    }



    /// <summary>
    /// This class do some helping functions
    /// </summary>
    public class StateMachineHelper : IDisposable
    {
        private IStateMachineHost _host;
        private Dictionary<BarItem, StateMachineUiTrigger> _triggerUiItems = new Dictionary<BarItem, StateMachineUiTrigger>();

        public StateMachineHelper(IStateMachineHost h)
        {
            _host = h;
        }

        public void RegisterItem(BarItem bi, string n)
        {
            _triggerUiItems.Add(bi, new StateMachineUiTrigger(bi, n));

            // connect to click
            bi.ItemClick += Bi_ItemClick;

        }

        public void Disable()
        {
            foreach (StateMachineUiTrigger t in _triggerUiItems.Values)
            {
                t.Item.Enabled = false;
            }
            Application.DoEvents();
        }

        public void Update()
        {
            // handle enabled state, all UI triggers are enabled only if current state has prper trigger
            foreach (StateMachineUiTrigger t in _triggerUiItems.Values)
            {
                t.Item.Enabled = _host.CurrentStateMachine.CurrentState.HasTrigger(t.TriggerName);
            }
            Application.DoEvents();
        }

        private void Bi_ItemClick(object sender, ItemClickEventArgs e)
        {
            StateMachineUiTrigger t = null;
            if(_triggerUiItems.TryGetValue(e.Item as BarItem, out t))
            {
                _host.CurrentStateMachine.CurrentState.GetTrigger(t.TriggerName).Fire();
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
                    foreach(StateMachineUiTrigger t in _triggerUiItems.Values) {
                        t.Item.ItemClick -= Bi_ItemClick;
                    }
                }

               
                disposedValue = true;
            }
        }


        // Questo codice viene aggiunto per implementare in modo corretto il criterio Disposable.
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

    }
}
