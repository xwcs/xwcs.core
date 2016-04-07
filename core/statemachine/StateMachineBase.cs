// Converting C:\xwee\gitRepos\database\support\DocumentStates\DocumentStates\DocumentStates.tastate into .cs file
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace xwcs.core.statemachine
{
    #region SyncClasses
    /// <summary>
    /// Syncronization Class.
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

    #region TransitionClasses
    /// <summary>
    /// Transition Event Class.
    /// </summary>
    public class TransitionEventArgs : EventArgs
    {
        public TransitionEventArgs(StateBase prev, StateBase next, TriggerBase why)
        {
            Prev = prev;
            Next = next;
            Why = why;
        }

		public override string ToString()
		{
			return string.Format("Transition event: [{0}] --({1})--> [{2}]", Prev?.Name ?? "", Why?.Name ?? "", Next?.Name ?? "");
		}

		public StateBase Prev { get; set; }
        public StateBase Next { get; set; }
        public TriggerBase Why { get; set; }
    }

    public delegate void TransitionEventHandler(object sender, TransitionEventArgs e) ;
	
    #endregion

    #region BaseClasses
    /// <summary>
    /// Trigger Exception (Condition failed or more).
    /// </summary>
    public class GuardException : ApplicationException
    {
        public GuardException(int code, string msg) : base(msg)
        {
            Code = code ;
        }

        public int Code { get; set; }
    }

    /// <summary>
    /// Effect Exception (Condition failed or more).
    /// </summary>
    public class EffectException : ApplicationException
    {
        public EffectException(int code, string msg) : base(msg)
        {
            Code = code;
        }

        public int Code { get; set; }
    }

    /// <summary>
    /// Base class for all Triggers that start a transition between states.
    /// </summary>
    public abstract class TriggerBase {
        public TriggerBase(StateMachine machine, string name)
        {
            this.StateMachine = machine ;
			Name = name;
        }

		public string Name { get; private set; } = "Trigger";

        /// <summary>
        /// The state machine this state belongs to.
        /// </summary>
        public StateMachine StateMachine { get; private set; }
    }

    /// <summary>
    /// Base class for all Triggers that start a transition between states.
    /// </summary>
    public abstract class GuardBase
    {
        public GuardBase(StateMachine machine)
        {
            this.StateMachine = machine;
        }

        public virtual void Execute()
        {
            // Will throw a GuardException when condition goes wrong
            return;
        }

        /// <summary>
        /// The state machine this state belongs to.
        /// </summary>
        public StateMachine StateMachine { get; private set; }
    }
    public class DefaultGuardBase : GuardBase {
        public DefaultGuardBase(StateMachine machine) : base(machine) { }
    };

    /// <summary>
    /// Base class for all States of the State Machine.
    /// </summary>
    public abstract class StateBase
	{ 
		/// <summary>
		/// Creates a new instance of this state with a reference to the state machine.
		/// </summary>
		public StateBase(StateMachine machine, string Name)
		{
			this.StateMachine = machine;
            this.Name = Name;
			this.Initialize();
		}
		/// <summary>
		/// The state machine this state belongs to.
		/// </summary>
		public StateMachine StateMachine { get; private set; }
        /// <summary>
        /// The state machine this state belongs to.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Initializes this state before the machine actually enters it.
        /// </summary>
        protected virtual void Initialize() 
		{
		}

        /// <summary>
        /// Returns a list of callable triggers
        /// </summary>
        public virtual List<TriggerBase> GetTriggers()
        {
            return new List<TriggerBase>();
        }

        /// <summary>
        /// Is executed when the state machine enters this state.
        /// </summary>
        public virtual void OnEntry(TriggerBase trigger) { }
        /// <summary>
        /// Is executed when the state machine leaves this state.
        /// </summary>
        public virtual void OnExit(TriggerBase trigger) { }
	}

    /// <summary>
    /// This state represents the start of the StateMachine.
    /// </summary>
    /* 
    public partial class StartState : StateBase
    {
        public StartState(StateMachine machine) : base(machine, "StartState") {}
        public override void OnEntry(TriggerBase causedByTrigger) { }
        public override void OnExit(TriggerBase causedByTrigger) { }
    }
    */

    /// <summary>
    /// Base class for the state machine. Implements main functionality.
    /// </summary>
    public abstract partial class StateMachine : INotifyPropertyChanged, IDisposable
    {
		public string Name { get; protected set; } = "unknown";

		/// <summary>
		/// Creates a new instance of this state machine.
		/// </summary>
		public StateMachine(ISynchronizeInvoke invokeDelegate)
		{
            _invokeDelegate = invokeDelegate;
            _CurrentState = null;
			this.Initialize();

            // Now start the Transition Consumer Thread
            _queue = new Queue<TriggerBase>();
            _syncEvents = new SyncEvents();
            _sync = new object();
            consumerThread = new Thread(ConsumerThread);
            consumerThread.Start();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                _syncEvents.ExitThreadEvent.Set();
                consumerThread.Join();

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~StateMachine() {
          // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
          Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

        /// <summary>
        /// Allows custom initailization code.
        /// </summary>
        protected virtual void Initialize() { }

        /// <summary>
        /// Start State call.
        /// </summary>
        public abstract void Start() ;

        /// <summary>
        /// Makes the state machine go into another state.
        /// </summary>
        public void TransitionToNewState(StateBase newState, TriggerBase causedByTrigger, GuardBase guard, TransitionEventHandler EffectHandler)
		{
            if (disposedValue)
            { // Silent
                throw new InvalidOperationException("State Machine Disposed");
            }
			// Pull the trigger to find if condition is Ok.
			OnTransitionEvent(StartTransition, this.CurrentState, newState, causedByTrigger);
            if ( guard != null )
            {
                guard.Execute();
            }

			OnTransitionEvent(BeforeExitingPreviousState, this.CurrentState, newState, causedByTrigger);
            // exit the current state
            if (this.CurrentState != null)
				this.CurrentState.OnExit(causedByTrigger);

            StateBase previousState = this.CurrentState ;
			this.CurrentState = newState;
			
			//call effect
			if(EffectHandler != null)
				OnTransitionEvent(EffectHandler, previousState, this.CurrentState, causedByTrigger);

            // enter the new state
            if (this.CurrentState != null)
				this.CurrentState.OnEntry(causedByTrigger);
			OnTransitionEvent(EndTransition, previousState, this.CurrentState, causedByTrigger);
        }

        private ISynchronizeInvoke _invokeDelegate;
        private StateBase _CurrentState;
        private Thread consumerThread = null;

        /// <summary>
        /// Gets the state the state machine is currently in.
        /// </summary>
        public StateBase CurrentState 
		{ 
			get {
                if (disposedValue)
                { // Silent
                    throw new InvalidOperationException("State Machine Disposed");
                }
                return _CurrentState;
            }
			private set
			{
				_CurrentState = value; 
				OnPropertyChanged(this, new PropertyChangedEventArgs("CurrentState"));
			}
		}

        /// <summary>
        /// Tells if the State Machine is started or set properly
        /// </summary>
        public bool IsWorking { get {
                if (disposedValue)
                { // Silent
                    throw new InvalidOperationException("State Machine Disposed");
                }
                return _CurrentState != null ;
        } }

        private Queue<TriggerBase> _queue;
        private SyncEvents _syncEvents;
        private readonly object _sync;

        /// <summary>
        /// Makes the state machine recive a command and dispatch it through the internal Queue.
        /// </summary>
        public void ProcessTrigger(TriggerBase trigger) {
            if (disposedValue)
            { // Silent
                throw new InvalidOperationException("State Machine Disposed");
            }
            lock (((ICollection)_queue).SyncRoot)
            {
                 _queue.Enqueue(trigger);
                 _syncEvents.NewTransitionEvent.Set();
            }
        }

        /// <summary>
        /// Makes the state machine processo a command. Depending on its current state
        /// and the designed transitions the machine reacts to the trigger.
        /// </summary>
        protected abstract void ProcessTriggerInternal(TriggerBase trigger);

        /// <summary>
        /// Internal Transition Thread.
        /// </summary>
        private void ConsumerThread()
        {
            while (WaitHandle.WaitAny(_syncEvents.EventArray) != 1)
            {
                lock (((ICollection)_queue).SyncRoot)
                {
                    TriggerBase t = _queue.Dequeue();
                    ProcessTriggerInternal(t) ;
                }
            }
            // Console.WriteLine("Consumer Thread: consumed {0} items", count);
        }


		//base events
        public event TransitionEventHandler StartTransition;
		public event TransitionEventHandler BeforeExitingPreviousState;
		public event TransitionEventHandler EndTransition;

		private void OnTransitionEvent(TransitionEventHandler handler, StateBase prev, StateBase next, TriggerBase why) {
			if (handler != null)
			{
				if (_invokeDelegate.InvokeRequired)
				{
					_invokeDelegate.Invoke(new TransitionEventHandler(handler), 
									new[] { this, (object) new TransitionEventArgs(prev, next, why) }
					);
					return;
				}
				handler(this, new TransitionEventArgs(prev, next, why));
			}
		}


        protected void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
//            if (this.PropertyChanged != null)
//                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));

            if (PropertyChanged != null)
            {
                if (_invokeDelegate.InvokeRequired)
                {
                    _invokeDelegate.Invoke(new PropertyChangedEventHandler(PropertyChanged), new[] { sender, e });
                    return;
                }
                PropertyChanged(this, e);
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

    }
	#endregion
}
// End of Template

