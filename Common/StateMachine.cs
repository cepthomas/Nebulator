using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;


namespace Nebulator.Common
{
    /// <summary>
    /// A generalized implementation of a state machine.
    /// </summary>
    /// <remarks>
    /// States:
    /// - Each state must have a name. 
    /// - Each state must have one or more Transitions.
    /// - Each state may have an enter and/or exit action executed on state changes.
    /// 
    /// Transitions:
    ///  - Each transition must have an event name.
    ///  - Each transition may have a next state name otherwise stays in the same state.
    ///  - Each transition may have a transition action.
    /// </remarks>

    public class StateMachine
    {
        #region Private fields
        /// <summary>All the states.</summary>
        Dictionary<string, State> _states = new Dictionary<string, State>();

        /// <summary>The event queue.</summary>
        Queue<EventInfo> _eventQueue = new Queue<EventInfo>();

        /// <summary>Queue serializing access.</summary>
        object _locker = new object();

        /// <summary>Flag to handle recursion in event processing.</summary>
        bool _processingEvents = false;
        #endregion

        #region Properties
        /// <summary>The machine current state.</summary>
        public State CurrentState { get; private set; } = null;

        /// <summary>Accumulated list of errors.</summary>
        public List<string> Errors { get; } = new List<string>();
        #endregion

        /// <summary>Definition for transition/entry/exit functions.</summary>
        /// <param name="o"></param>
        public delegate void SmFunc(object o);

        /// <summary>
        /// Init everything. Also does validation of the definitions at the same time.
        /// </summary>
        /// <param name="states">All the states.</param>
        /// <param name="initialState">Initial state.</param>
        /// <returns>Initialization success.</returns>
        public bool Init(State[] states, string initialState)
        {
            Errors.Clear();
            _states.Clear();
            _eventQueue.Clear();

            try
            {
                ////// Populate our collection from the client.
                foreach (State st in states)
                {
                    if (st.Transitions is null || st.Transitions.Count == 0)
                    {
                        string serr = $"No transitions for State:{st.StateName}";
                        Errors.Add(serr);
                    }
                    else
                    {
                        // Check for duplicate state names.
                        if (!_states.ContainsKey(st.StateName))
                        {
                            _states.Add(st.StateName, st);
                        }
                        else
                        {
                            string serr = $"Duplicate State Name:{st.StateName}";
                            Errors.Add(serr);
                        }
                    }
                }

                //////// Sanity checking on the transitions.
                foreach (State st in _states.Values)
                {
                    Errors.AddRange(st.Init(_states.Keys));
                }

                if (initialState != null && _states.ContainsKey(initialState))
                {
                    CurrentState = _states[initialState];
                    CurrentState.Enter(null);
                }
                else // invalid initial state
                {
                    string serr = $"Invalid Initial State:{initialState}";
                    Errors.Add(serr);
                }
            }
            catch (Exception e)
            {
                string serr = $"Exception during initializing:{e.Message} ({e.StackTrace})";
                Errors.Add(serr);
            }

            return Errors.Count == 0;
        }

        /// <summary>
        /// Processes an event. Returns when event queue is empty.
        /// Events can be coming on different threads so this method is locked.
        /// </summary>
        /// <param name="evt">Incoming event.</param>
        /// <param name="o">Optional event data.</param>
        /// <returns>Ok or error.</returns>
        public bool ProcessEvent(string evt, object o = null)
        {
            bool ok = true;

            lock (_locker)
            {
                // Add the event to the queue.
                _eventQueue.Enqueue(new EventInfo() { Name = evt, Param = o });

                // Check for recursion through the processing loop - event may be generated internally during processing.
                if (!_processingEvents)
                {
                    _processingEvents = true;

                    // Process all events in the event queue.
                    while (_eventQueue.Count > 0 && ok)
                    {
                        EventInfo ei = _eventQueue.Dequeue();
                        try
                        {
                            // Dig out the correct transition if there is one.

                            // Try current state.
                            string nextStateName = CurrentState.ProcessEvent(ei);

                            if (nextStateName is null)
                            {
                                throw new Exception($"State: {CurrentState.StateName} InvalidEvent: {ei.Name}");
                            }

                            // is there a state change?
                            if (nextStateName != CurrentState.StateName)
                            {
                                // get the next state
                                State nextState = _states[nextStateName];

                                // Exit current state.
                                CurrentState.Exit(ei.Param);

                                // Set new state.
                                CurrentState = nextState;

                                // Enter new state.
                                CurrentState.Enter(ei.Param);
                            }
                        }
                        catch (Exception e)
                        {
                            // Add to the list of errors.
                            Errors.Add(e.Message);

                            // Set the return status.
                            ok = false;

                            // Clean up.
                            _eventQueue.Clear();
                            _processingEvents = false;

                            // Rethrow.
                            //throw;
                        }
                    }
                }

                // Done for now.
                _processingEvents = false;

                return ok;
            }
        }

        /// <summary>
        /// Generate DOT markup
        /// </summary>
        /// <returns>Returns a string that contains the DOT markup.</returns>
        public string GenerateDot()
        {
            List<string> ls = new List<string>
            {
                "digraph StateDiagram {",

                // Init attributes for dot.
                "    ratio=\"compress\";",
                "    fontname=\"Arial\";",
                "    label=\"\";", // (your label here!)

                "    node [",
                "    height=\"0.50\";",
                "    width=\"1.0\";",
                "    shape=\"ellipse\";",
                "    fixedsize=\"true\";",
                "    fontsize=\"8\";",
                "    fontname=\"Arial\";",
                "];",
                "",
                "    edge [",
                "    fontsize=\"8\";",
                "    fontname=\"Arial\";",
                "];",
                ""
            };

            // Generate actual nodes and edges from states
            foreach (State s in _states.Values)
            {
                // Write a node for the state.
                ls.Add($"    \"{s.StateName}\";\r\n");

                // Iterate through the state transitions.
                foreach (KeyValuePair<string, Transition> kvp in s.Transitions)
                {
                    Transition t = kvp.Value;

                    // Get event name, but strip off "Transition" suffix if present to save space.
                    string transitionSuffix = "Transition";
                    string eventName = t.EventName;
                    if (eventName.EndsWith(transitionSuffix))
                    {
                        eventName = eventName.Substring(0, eventName.Length - transitionSuffix.Length);
                    }

                    // Write an edge for the transition
                    string nextState = t.NextState;
                    if (nextState == "SAME_STATE")
                    {
                        nextState = s.StateName;
                    }
                    ls.Add($"        \"{s.StateName}\" -> \"{nextState}\" [label=\"{eventName}\"];\r\n");
                }

                ls.Add("{0}");
            }

            ls.Add("};");

            return string.Join(Environment.NewLine, ls);
        }
    }

    /// <summary>Describes an individual state.</summary>
    public class State
    {
        #region Properties
        /// <summary>The state name.</summary>
        public string StateName { get; } = Utils.UNKNOWN_STRING;

        /// <summary>All the transitions possible for this state.</summary>
        public Dictionary<string, Transition> Transitions { get; set; } = new Dictionary<string, Transition>();
        #endregion

        #region Private fields
        /// <summary>Convenience reference to optional default transition.</summary>
        private Transition _defaultTransition = null;

        /// <summary>Optional state entry action.</summary>
        private StateMachine.SmFunc _entryFunc = null;

        /// <summary>Optional state exit action.</summary>
        private StateMachine.SmFunc _exitFunc = null;
        #endregion

        #region Constructor
        /// <summary>Main constructor.</summary>
        /// <param name="st">Associated state name</param>
        /// <param name="entry">Optional state entry action</param>
        /// <param name="exit">Optional state exit action</param>
        /// <param name="transitions">Collection of transitions for this state</param>
        public State(string st, StateMachine.SmFunc entry, StateMachine.SmFunc exit, params Transition[] transitions)
        {
            StateName = st;
            _entryFunc = entry;
            _exitFunc = exit;

            // Copy the transitions temporarily, ignoring the event names for now.
            Transitions = new Dictionary<string, Transition>();
            for (int i = 0; i < transitions.Count(); i++)
            {
                Transitions.Add(i.ToString(), transitions[i]);
            }
        }
        #endregion

        #region Public methods
        /// <summary>Initialize the state and its transitions.</summary>
        /// <param name="stateNames">All valid state names</param>
        /// <returns>List of any errors.</returns>
        public List<string> Init(IEnumerable<string> stateNames)
        {
            List<string> errors = new List<string>();

            // Adjust transitions for DEFAULT_EVENT and SAME_STATE conditions.
            // First take a copy of the current.
            Dictionary<string, Transition> tempTrans = Transitions;

            Transitions = new Dictionary<string, Transition>();

            foreach (Transition t in tempTrans.Values)
            {
                if (t.EventName == "")
                {
                    if (_defaultTransition is null)
                    {
                        _defaultTransition = t;
                    }
                    else
                    {
                        string serr = $"Duplicate Default Event defined for:{StateName}";
                        errors.Add(serr);
                    }
                }
                else
                {
                    if (!Transitions.ContainsKey(t.EventName))
                    {
                        Transitions.Add(t.EventName, t);
                    }
                    else
                    {
                        string serr = $"Duplicate Event Name:{t.EventName}";
                        errors.Add(serr);
                    }
                }

                // Fix any SAME_STATE to current.
                string nextState = t.NextState;
                if (nextState == "")
                {
                    t.NextState = StateName;
                }

                // Is the nextState valid?
                if (!stateNames.Contains(t.NextState))
                {
                    string serr = $"Undefined NextState:{ t.NextState}";
                    errors.Add(serr);
                }
            }

            return errors;
        }

        /// <summary>Process the event.</summary>
        /// <param name="ei">The event information.</param>
        /// <returns>The next state.</returns>
        public string ProcessEvent(EventInfo ei)
        {
            string nextState = null;

            if (Transitions != null)
            {
                // Get the transition associated with the event
                if (!Transitions.TryGetValue(ei.Name, out Transition tx))
                {
                    tx = _defaultTransition;
                }

                // Execute transition if found, otherwise return the null and let the caller handle it.
                if (tx != null)
                {
                    nextState = tx.Execute(ei);
                }
            }

            return nextState;
        }

        /// <summary>Enter the state by executing the enter action</summary>
        /// <param name="o">Optional data object</param>
        /// <returns>void</returns>
        public void Enter(object o)
        {
            _entryFunc?.Invoke(o);
        }

        /// <summary>Exit the state by executing the enter action</summary>
        /// <param name="o">Optional data object</param>
        /// <returns>void</returns>
        public void Exit(object o)
        {
            _exitFunc?.Invoke(o);
        }
        #endregion
    }

    /// <summary>Describes an individual transition.</summary>
    public class Transition
    {
        /// <summary>The name of the event that triggers this transition.</summary>
        public string EventName { get; } = Utils.UNKNOWN_STRING;

        /// <summary>Change state to this after execution action.</summary>
        public string NextState { get; set; } = Utils.UNKNOWN_STRING;

        /// <summary>Optional action - executed before state change</summary>
        public StateMachine.SmFunc TransitionFunc { get; set; } = null;

        /// <summary>Constructor.</summary>
        /// <param name="evt">Incoming event name</param>
        /// <param name="nextState">Next state name</param>
        /// <param name="trans">Optional transition action</param>
        public Transition(string evt, string nextState = "", StateMachine.SmFunc trans = null)
        {
            EventName = evt;
            NextState = nextState;
            TransitionFunc = trans;
        }

        /// <summary>Execute transition action.</summary>
        /// <param name="ei">Event information</param>
        /// <returns>The next state</returns>
        public string Execute(EventInfo ei)
        {
            TransitionFunc?.Invoke(ei.Param);
            return NextState;
        }
    }

    /// <summary>Data carrying class.</summary>
    public class EventInfo
    {
        /// <summary>Unique event name.</summary>
        public string Name { get; set; } = Utils.UNKNOWN_STRING;

        /// <summary>Event data.</summary>
        public object Param { get; set; } = null;

        /// <summary>Generate a human readable string.</summary>
        public override string ToString()
        {
            return $"Event:{Name} Param:{Param ?? "null"}";
        }
    }
}
