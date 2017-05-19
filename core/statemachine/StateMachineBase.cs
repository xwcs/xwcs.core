using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Threading;
using xwcs.core.evt;
using System.Runtime.CompilerServices;

namespace xwcs.core.statemachine
{

    #region State machine context
    /// <summary>
    /// This class will hold any state machine created weak reference
    /// So we guaranties that when we go kill app it will kill anything
    /// </summary>
    public class StateMachinesDisposer : IDisposable
    {
        private List<WeakReference<StateMachine>> _machines = new List<WeakReference<StateMachine>>();

        private long _counter = 0;


        public void RegisterSM(StateMachine sm)
        {
            _machines.Add(new WeakReference<StateMachine>(sm));
            ++_counter;
        }

        public void UnRegisterSM(StateMachine sm)
        {
            // machine will be destroyed anyway so we just remove count
            --_counter;
        }
        private static StateMachinesDisposer instance;

        //singleton need private ctor
        private StateMachinesDisposer() { }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static StateMachinesDisposer getInstance()
        {
            if (instance == null)
            {
                instance = new StateMachinesDisposer();
            }
            return instance;
        }

        #region IDisposable Support
        private bool disposedValue = false; // Per rilevare chiamate ridondanti

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach(WeakReference<StateMachine> s in _machines)
                    {
                        StateMachine tsm = null;
                        if (s.TryGetTarget(out tsm))
                        {
                            tsm.Dispose();
                        }
                    }
                }

#if DEBUG_TRACE_LOG_ON
                Console.WriteLine("State machine count on exit : " + _counter.ToString()); 
#endif
                disposedValue = true;
            }
        }

        // Questo codice viene aggiunto per implementare in modo corretto il criterio Disposable.
        public void Dispose()
        {
            // Non modificare questo codice. Inserire il codice di pulizia in Dispose(bool disposing) sopra.
            Dispose(true);
            // TODO: rimuovere il commento dalla riga seguente se è stato eseguito l'override del finalizzatore.
            // GC.SuppressFinalize(this);
        }
        #endregion


    }
    #endregion

    /*
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
    */

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

        /// <summary>
        /// This function will fire trigger on state machine
        /// </summary>
        public void Fire()
        {
            this.StateMachine.ProcessTrigger(this);
        } 
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

        public virtual bool Execute()
        {
            return true;
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
        private Dictionary<string, TriggerBase> _triggers = new Dictionary<string, TriggerBase>();

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
        protected virtual void Initialize(){}

        /// <summary>
        /// Make triggers
        /// </summary>
        protected virtual void InitTriggers(){}
        private void InitTriggersInternal(){
            if(_triggers.Count == 0)
            {
                InitTriggers();
            }
        }

        /// <summary>
        /// Returns a list of callable triggers
        /// </summary>
        public virtual IReadOnlyCollection<TriggerBase> Triggers
        {
            get
            {
                InitTriggersInternal();
                return _triggers.Values;
            }           
        }

        /// <summary>
        /// Check presence of some triger by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual bool HasTrigger(string name)
        {
            InitTriggersInternal();
            return _triggers.ContainsKey(name + "Trigger");
        }


        /// <summary>
        /// Get trigger by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual TriggerBase GetTrigger(string name)
        {
            InitTriggersInternal();
            name += "Trigger";
            if (_triggers.ContainsKey(name))
            {
                return _triggers[name];
            }
            
            throw new ApplicationException(string.Format("State {0} has no trigger {1}!", this.Name, name));
        }

        protected void AddTrigger(TriggerBase t)
        {
            if(!_triggers.ContainsKey(t.Name))
                _triggers[t.Name] = t;
        }

        public void Fire(string triggerName)
        {
            GetTrigger(triggerName).Fire();
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
    /// Base class for all Conditional States of the State Machine.
    /// </summary>
    public abstract class ConditionStateBase : StateBase
    {
        /// <summary>
        /// Creates a new instance of this state with a reference to the state machine.
        /// </summary>
        public ConditionStateBase(StateMachine machine, string Name) : base(machine, Name) { }

        /// <summary>
        /// Is executed when the state machine enters this state.
        /// </summary>
        public override void OnEntry(TriggerBase trigger) {
            // send all triggers
            this.StateMachine.ProcessTriggers(Triggers);
        }
    }

    
    /// <summary>
    /// Base interface for all hosts
    /// </summary>
    public interface IStateMachineHost
    {
        StateMachine GetNewStateMachine();
        StateMachine CurrentStateMachine { get; }
    }


    /// <summary>
    /// Base class for the state machine. Implements main functionality.
    /// </summary>
    public abstract partial class StateMachine : INotifyPropertyChanged, IDisposable
    {
		public string Name { get; protected set; } = "unknown";

        

        private IStateMachineHost _host;
        public IStateMachineHost Host
        {
            get
            {
                return _host;
            }
        }


        
        /// <summary>
        /// Creates a new instance of this state machine.
        /// </summary>
        public StateMachine(IStateMachineHost host)
		{
            _host = host;
            // register for dispose
            StateMachinesDisposer.getInstance().RegisterSM(this);

            _CurrentState = null;
            _wes_PropertyChanged = new WeakEventSource<PropertyChangedEventArgs>();
            _wes_StartTransition = new WeakEventSource<TransitionEventArgs>();
            _wes_BeforeExitingPreviousState = new WeakEventSource<TransitionEventArgs>();
            _wes_EndTransition = new WeakEventSource<TransitionEventArgs>();

            this.Initialize();

            _queue = new Queue<object>();
            
            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 1;
            _timer.Tick += _timer_Tick;
            _timer.Enabled = false;

            _effectsQueue = new Queue<EffectAction>();
            _effectsTimer = new System.Windows.Forms.Timer();
            _effectsTimer.Interval = 1;
            _effectsTimer.Tick += _effectsTimer_Tick; ;
            _effectsTimer.Enabled = false;
        }

       




        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _wes_PropertyChanged = null;
                    _wes_StartTransition = null;
                    _wes_BeforeExitingPreviousState = null;
                    _wes_EndTransition = null;
                    _timer.Tick -= _timer_Tick;
                    _timer.Dispose();
                    _effectsTimer.Tick -= _effectsTimer_Tick;
                    _effectsTimer.Dispose();
                }

                // decrement
                StateMachinesDisposer.getInstance().UnRegisterSM(this);

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
        /// Allows custom initialization code.
        /// </summary>
        protected virtual void Initialize() { }

        /// <summary>
        /// Start State call.
        /// </summary>
        public abstract void Start() ;

        /// <summary>
        /// Makes the state machine go into another state.
        /// </summary>
        /// <param name="newState"></param>
        /// <param name="causedByTrigger"></param>
        /// <param name="guard"></param>
        /// <param name="EffectHandler"></param>
        /// <returns>true if done</returns>
        public bool TransitionToNewState(StateBase newState, TriggerBase causedByTrigger, GuardBase guard, WeakEventSource<TransitionEventArgs> EffectHandler)
		{
            if (disposedValue)
            { // Silent
                throw new InvalidOperationException("State Machine Disposed");
            }
            // Pull the trigger to find if condition is Ok.
            _wes_StartTransition?.Raise(this, new TransitionEventArgs(CurrentState, newState, causedByTrigger));
			if ( guard != null )
            {
                if (!guard.Execute()) return false; // Guard said this trigger can't go on
            }

			_wes_BeforeExitingPreviousState?.Raise(this, new TransitionEventArgs(CurrentState, newState, causedByTrigger));
            // exit the current state
            if (this.CurrentState != null)
				this.CurrentState.OnExit(causedByTrigger);

            StateBase previousState = this.CurrentState ;
			this.CurrentState = newState;
			
			//call effect
			if(EffectHandler != null)
            {
                ProcessEffect(new EffectAction(EffectHandler, new TransitionEventArgs(previousState, newState, causedByTrigger)));
            }

            // Process Effect can delete SM itself so handle it
            if (disposedValue) return false; // we are over end of life just exit
                
            // enter the new state
            if (this.CurrentState != null)
				this.CurrentState.OnEntry(causedByTrigger);

            _wes_EndTransition?.Raise(this, new TransitionEventArgs(previousState, newState, causedByTrigger));

            // we did well so ok send true
            return true;
        }

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
                if (disposedValue)
                { // Silent
                    throw new InvalidOperationException("State Machine Disposed");
                }
                _CurrentState = value; 
                if ( !(_CurrentState is ConditionStateBase) )
                {
                    _wes_PropertyChanged?.Raise(this, new PropertyChangedEventArgs("CurrentState"));
                }
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


        // triggers 
        private Queue<object> _queue; // it can hold List<TriggerBase> or TriggerBase
        // this will fire i 1ms we just need to split execution chain
        private System.Windows.Forms.Timer _timer = null;


        struct EffectAction
        {
            WeakEventSource<TransitionEventArgs> EffectHandler;
            TransitionEventArgs Args;

            public EffectAction(WeakEventSource<TransitionEventArgs> e, TransitionEventArgs a)
            {
                EffectHandler = e;
                Args = a;
            }
            
            public void Fire()
            {
                EffectHandler.Raise(this, Args);
            }
        }

        // effects 
        private Queue<EffectAction> _effectsQueue;
        // this will fire i 1ms we just need to split execution chain
        private System.Windows.Forms.Timer _effectsTimer = null;

        

        /// <summary>
        ///  Makes the state machine recive a command and dispatch it through the internal Queue.
        /// </summary>
        /// <param name="trigger"></param>
        public void ProcessTrigger(TriggerBase trigger) {
            if (disposedValue)
            { // Silent
                throw new InvalidOperationException("State Machine Disposed");
            }            
            _queue.Enqueue(trigger);
            _timer.Start();
            Application.DoEvents();
        }
        public void ProcessTriggers(IReadOnlyCollection<TriggerBase> triggers)
        {
            if (disposedValue)
            { // Silent
                throw new InvalidOperationException("State Machine Disposed");
            }
            _queue.Enqueue(triggers);
            _timer.Start();
            Application.DoEvents();
        }
        private void ConsumeQueue()
        {
            if (_queue.Count > 0)
            {
                object tt = _queue.Dequeue();
                if(tt is IReadOnlyCollection<TriggerBase>)
                {
                    foreach(TriggerBase t in (tt as IReadOnlyCollection<TriggerBase>))
                    {
                        if (ProcessTriggerInternal(t as TriggerBase)) return; // we done here we skip others after first good
                    }
                }else
                {
                    ProcessTriggerInternal(tt as TriggerBase);
                }                
            }
            if (_queue.Count > 0)
            {
                _timer.Start();
            }
        }
        private void _timer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();
            ConsumeQueue();
        }

        /// <summary>
        /// Receive Effect event raise request and dispatch it through the internal Queue.
        /// </summary>
        private void ProcessEffect(EffectAction ef)
        {
            if (disposedValue)
            { // Silent
                throw new InvalidOperationException("State Machine Disposed");
            }

            _effectsQueue.Enqueue(ef);
            _effectsTimer.Start();
            Application.DoEvents();
        }
        private void ConsumeEffectQueue()
        {
            if (_effectsQueue.Count > 0)
            {
                EffectAction ef = _effectsQueue.Dequeue();
                ef.Fire();
                // during this event SM can be eventually disposed from outside!!!!!
                if (disposedValue) return;
            }
            if (_effectsQueue.Count > 0)
            {
                _effectsTimer.Start();
            }
        }
        private void _effectsTimer_Tick(object sender, EventArgs e)
        {
            _effectsTimer.Stop();
            ConsumeEffectQueue();
        }

        /// <summary>
        /// Makes the state machine process a command. Depending on its current state
        /// and the designed transitions the machine reacts to the trigger.
        /// </summary>
        protected abstract bool ProcessTriggerInternal(TriggerBase trigger);

        
        // base events
        private WeakEventSource<TransitionEventArgs> _wes_StartTransition = null;
        public event EventHandler<TransitionEventArgs> StartTransition
        {
            add { _wes_StartTransition.Subscribe(value); }
            remove { _wes_StartTransition.Unsubscribe(value); }
        }
        private WeakEventSource<TransitionEventArgs> _wes_BeforeExitingPreviousState = null;
        public event EventHandler<TransitionEventArgs> BeforeExitingPreviousState
        {
            add { _wes_BeforeExitingPreviousState.Subscribe(value); }
            remove { _wes_BeforeExitingPreviousState.Unsubscribe(value); }
        }
        private WeakEventSource<TransitionEventArgs> _wes_EndTransition = null;
        public event EventHandler<TransitionEventArgs> EndTransition
        {
            add { _wes_EndTransition.Subscribe(value); }
            remove { _wes_EndTransition.Unsubscribe(value); }
        }
        
        private WeakEventSource<PropertyChangedEventArgs> _wes_PropertyChanged = null;
        public event PropertyChangedEventHandler PropertyChanged
        {
            add{ _wes_PropertyChanged.SubscribePropertyChanged(value); }
            remove{ _wes_PropertyChanged.UnsubscribePropertyChanged(value); }
        }
    }
	#endregion
}
// End of Template

