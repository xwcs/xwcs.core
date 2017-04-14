using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using xwcs.core.evt;
using xwcs.core.statemachine;

namespace xwcs.core.statemachine.test
{
    public partial class TestFormBase : Form
    {

		protected void Log(string msg) {
			richTextBox1.AppendText(msg + Environment.NewLine);
			richTextBox1.SelectionStart = richTextBox1.Text.Length;
			// scroll it automatically
			richTextBox1.ScrollToCaret();
		}

		private StateMachine _machine;
		
		protected virtual StateMachine CreateMachine()
		{
			return null; //must be implemented!
		}

		public TestFormBase()
        {
            // connect to StateMachine context
            SEventProxy.InvokeDelegate = this;

            InitializeComponent();
			
			if(!DesignMode) {
				_machine = CreateMachine();
				
				if (_machine == null) return;

				_machine.StartTransition += (object s, TransitionEventArgs e) => { Log("Before transition :" + e + "  called."); };
				_machine.BeforeExitingPreviousState += (object s, TransitionEventArgs e) => { Log("BeforeExit : " + e + "  called."); };
				_machine.EndTransition += (object s, TransitionEventArgs e) => { Log("End transition :" + e + "  called."); };

				Text = "State machine : " + _machine.Name;

				this.FormClosing += (s, e) =>
				{
					_machine.Dispose();
				};

				_machine.PropertyChanged += DocState_PropertyChanged;
			}			
        }

        private List<Button> _myButtons = new List<Button>() ;
        
        private void AddButtons()
        {
            foreach (Button b in _myButtons)
            {
                panel1.Controls.Remove(b);
                b.Dispose(); // As delete.
            }

            _myButtons.Clear();

            // Ask for available triggers.
            foreach (TriggerBase t in _machine.CurrentState.Triggers)
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

        private void button1_Click(object sender, EventArgs e)
        {
			_machine.Start();
        }
    }
}
