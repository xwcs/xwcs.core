using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using xwcs.core.statemachine;

namespace xwcs.core.statemachine.test
{
    public partial class TestFormBase : Form
    {

		private StateMachine _machine;
		
		protected virtual StateMachine CreateMachine()
		{
			return null; //must be implemented!
		}

		public TestFormBase()
        {
            InitializeComponent();
			//DocState = new DocumentState(this);
			_machine = CreateMachine();

			Text = "State machine : " + _machine.Name;

			this.FormClosing += (s, e) =>
            {
				_machine.Dispose();
            };

			//_propertyChangedNH = new xwcs.core.statemachine.NotificationHelper(this, DocState, DocState_PropertyChanged);
			_machine.PropertyChanged += DocState_PropertyChanged;
        }

        private List<Button> _myButtons = new List<Button>() ;
        // private xwcs.core.statemachine.NotificationHelper _propertyChangedNH;

        private void AddButtons()
        {
            foreach (Button b in _myButtons)
            {
                panel1.Controls.Remove(b);
                b.Dispose(); // As delete.
            }

            _myButtons.Clear();

            // Ask for available triggers.
            List<TriggerBase> tList = _machine.CurrentState.GetTriggers();

            foreach (TriggerBase t in tList)
            {
                Button newButton = new Button();
                newButton.Text = t.GetType().Name;
                newButton.Click += (s, e) =>
                {
					_machine.ProcessTrigger(t);
                } ;
                _myButtons.Add(newButton);
                panel1.Controls.Add(newButton);
            }
        }

        private void DocState_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ( e.PropertyName == "CurrentState")
            {
                label1.Text = _machine.CurrentState.Name;
                AddButtons();
            }
            // else don't care
        }

        /*
        private void DocState_EventChanged(object sender, TransitionEventArgs e)
        {
            if (e.Prev is CorrezioneState && e.Next is ConsolidatoState)
            {
                var confirmResult = MessageBox.Show("Pay attention, Event from CorrezioneState to ConsolidatoState occurred",
                        "Event Handler", MessageBoxButtons.OK);
            }
            // else don't care
        }
        */

        private void button1_Click(object sender, EventArgs e)
        {
			_machine.Start();
        }
    }
}
