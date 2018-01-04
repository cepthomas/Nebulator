using System;
using System.Collections.Generic;
using System.Linq;
using Nebulator.Common;


namespace Nebulator.Test
{
    public class SM_1 : TestCase
    {
        public override void RunCase()
        {
            UT_INFO("Test the full StateMachine using a real world example.");

            // Create a new lock.
            CombinationLock mainDoorLock = new CombinationLock(CombinationLock.HwLockStates.HwIsLocked);
            mainDoorLock.InitStateMachine();

            // Should come up in the locked state.
            UT_STR_EQUAL(mainDoorLock.CurrentState, "Locked");

            // Enter the default combination of 000.
            mainDoorLock.PressKey(CombinationLock.Keys.Key_0);
            UT_STR_EQUAL(mainDoorLock.CurrentState, "Locked");
            mainDoorLock.PressKey(CombinationLock.Keys.Key_0);
            UT_STR_EQUAL(mainDoorLock.CurrentState, "Locked");
            mainDoorLock.PressKey(CombinationLock.Keys.Key_0);

            // Should now be unlocked.
            UT_STR_EQUAL(mainDoorLock.CurrentState, "Unlocked");

            // Test the default handler. Should stay in the same state.
            mainDoorLock.PressKey(CombinationLock.Keys.Key_5);
            UT_STR_EQUAL(mainDoorLock.CurrentState, "Unlocked");

            // Lock it again.
            mainDoorLock.PressKey(CombinationLock.Keys.Key_Reset);
            UT_STR_EQUAL(mainDoorLock.CurrentState, "Locked");

            // Unlock it again.
            mainDoorLock.PressKey(CombinationLock.Keys.Key_0);
            mainDoorLock.PressKey(CombinationLock.Keys.Key_0);
            mainDoorLock.PressKey(CombinationLock.Keys.Key_0);
            UT_STR_EQUAL(mainDoorLock.CurrentState, "Unlocked");

            // Must be in the unlocked state to change the combination.
            // Press set, new combo, set, set the combination to 123.
            mainDoorLock.PressKey(CombinationLock.Keys.Key_Set);
            UT_STR_EQUAL(mainDoorLock.CurrentState, "SettingCombo");

            UT_EQUAL(mainDoorLock.SM.ProcessEvent("NGEVENT"), false);

            UT_STR_EQUAL(mainDoorLock.CurrentState, "SettingCombo");

            // The state machine is now dead and will no longer process events.
            UT_GREATER(mainDoorLock.SM.Errors.Count, 0);

            mainDoorLock.PressKey(CombinationLock.Keys.Key_1);
            mainDoorLock.PressKey(CombinationLock.Keys.Key_2);
            mainDoorLock.PressKey(CombinationLock.Keys.Key_3);
            UT_STR_EQUAL(mainDoorLock.CurrentState, "SettingCombo");

            mainDoorLock.PressKey(CombinationLock.Keys.Key_Set);

            UT_STR_EQUAL(mainDoorLock.CurrentState, "Unlocked");
        }
    }

    /// <summary>The CombinationLock class provides both an example and a test of the VSP state machine classes.</summary>
    public class CombinationLock
    {
        #region Private fields
        /// <summary>Current combination.</summary>
        private List<Keys> _combination = new List<Keys>();

        /// <summary>Where we are in the sequence.</summary>
        private List<Keys> _currentEntry = new List<Keys>();
        #endregion

        #region Test support public
        /// <summary>Readable version of current state for testing.</summary>
        /// <value>The state name.</value>
        public string CurrentState
        {
            get { return SM.CurrentState.StateName; }
        }

        /// <summary>Accessor to the tested StateMachine.</summary>
        /// <value>The state machine object.</value>
        public StateMachine SM { get; set; }

        /// <summary>Input from the keypad</summary>
        /// <param name="key">Key pressed on the keypad</param>
        /// <returns>void</returns>
        public void PressKey(Keys key)
        {
            Console.WriteLine($"KeyPressed:{key}");

            switch (key)
            {
                case Keys.Key_Reset:
                    SM.ProcessEvent("Reset", key);
                    break;

                case Keys.Key_Set:
                    SM.ProcessEvent("SetCombo", key);
                    break;

                default:
                    SM.ProcessEvent("DigitKeyPressed", key);
                    break;
            }
        }
        #endregion

        #region The State Machine
        /// <summary>Initialize the state machine.</summary>
        /// <returns>void</returns>
        public void InitStateMachine()
        {
            State[] states = new State[]
            {
                new State("Initial", InitialEnter, InitialExit,
                    new Transition("IsLocked", "Locked"),
                    new Transition("IsUnlocked", "Unlocked")),

                new State("Locked", LockedEnter, null,
                    new Transition("ForceFail", "", ForceFail),
                    new Transition("DigitKeyPressed", "", LockedAddDigit),
                    new Transition("Reset", "", ClearCurrentEntry),
                    new Transition("ValidCombo", "Unlocked"),
                    new Transition("", "", ClearCurrentEntry)), // ignore other events
                
                new State("Unlocked", UnlockedEnter, null,
                    new Transition("Reset", "Locked", ClearCurrentEntry),
                    new Transition("SetCombo", "SettingCombo", ClearCurrentEntry),
                    new Transition("", "", ClearCurrentEntry)), // ignore other events
                
                new State("SettingCombo", ClearCurrentEntry, null,
                    new Transition("DigitKeyPressed", "", SetComboAddDigit),
                    new Transition("SetCombo", "Unlocked", SetCombo),
                    new Transition("Reset", "Unlocked", ClearCurrentEntry)
                    )
            };

            // initialize the state machine
            bool stateMachineIsValid = SM.Init(states, "Initial");
        }
        #endregion

        #region Keypad enum
        /// <summary>Standard 12-key keypad: 0-9, *, and # keys.</summary>
        public enum Keys
        {
            Key_0 = '0',
            Key_1,
            Key_2,
            Key_3,
            Key_4,
            Key_5,
            Key_6,
            Key_7,
            Key_8,
            Key_9,
            Key_Reset = '*',
            Key_Set = '#',
        }
        #endregion

        #region Physical Lock
        /// <summary>State of the HW lock</summary>
        public enum HwLockStates
        {
            HwIsLocked,
            HwIsUnlocked
        }

        /// <summary>Current state of the HW Lock</summary>
        /// <value>The state</value>
        private HwLockStates HwLockState { get; set; }

        /// <summary>Energize the HW lock to the locked position</summary>
        /// <returns>void</returns>
        private void HwLock()
        {
            Console.WriteLine("HwLock: Locking");
            HwLockState = HwLockStates.HwIsLocked;
        }

        /// <summary>Energize the HW lock to the unlocked position</summary>
        /// <returns>void</returns>
        private void HwUnLock()
        {
            Console.WriteLine("HwLock: Unlocking");
            HwLockState = HwLockStates.HwIsUnlocked;
        }
        #endregion

        #region Construction
        /// <summary>Normal constructor.</summary>
        /// <param name="hwLockState">Initialize state</param>
        public CombinationLock(HwLockStates hwLockState)
        {
            // Create the FSM.
            SM = new StateMachine();

            HwLockState = hwLockState; // initialize the state of the HW lock

            _currentEntry = new List<Keys>();

            // initial combination is: 000
            _combination = new List<Keys>();
            _combination.Add(Keys.Key_0);
            _combination.Add(Keys.Key_0);
            _combination.Add(Keys.Key_0);
        }
        #endregion

        #region Transition functions
        /// <summary>Initialize the lock</summary>
        /// <returns>void</returns>
        private void InitialEnter(Object o)
        {
            Console.WriteLine($"InitialEnter:{o}");
            if (HwLockState == HwLockStates.HwIsLocked)
            {
                SM.ProcessEvent("IsLocked");
            }
            else
            {
                SM.ProcessEvent("IsUnlocked");
            }
        }

        /// <summary>Dummy function</summary>
        /// <returns>void</returns>
        private void InitialExit(Object o)
        {
            Console.WriteLine($"InitialExit:{o}");
        }

        /// <summary>Locked transition function.</summary>
        /// <returns>void</returns>
        private void LockedEnter(Object o)
        {
            Console.WriteLine($"LockedEnter:{o}");
            HwLock();
            _currentEntry.Clear();
        }

        /// <summary>Clear the lock</summary>
        /// <returns>void</returns>
        private void ClearCurrentEntry(Object o)
        {
            Console.WriteLine($"ClearCurrentEntry:{o}");
            _currentEntry.Clear();
        }

        /// <summary>Add a digit to the current sequence.</summary>
        /// <returns>void</returns>
        private void LockedAddDigit(Object o)
        {
            Console.WriteLine($"LockedAddDigit:{o}");
            Keys key = (Keys)o;

            _currentEntry.Add(key);
            if (_currentEntry.SequenceEqual(_combination))
            {
                SM.ProcessEvent("ValidCombo");
            }
        }

        /// <summary>Add a digit to the current sequence.</summary>
        /// <returns>void</returns>
        private void SetComboAddDigit(Object o)
        {
            Console.WriteLine($"SetComboAddDigit:{o}");
            Keys key = (Keys)o;

            _currentEntry.Add(key);
        }

        /// <summary>Try setting a new combination.</summary>
        /// <returns>void</returns>
        private void SetCombo(Object o)
        {
            Console.WriteLine($"SetCombo:{o}");
            if (_currentEntry.Count > 0)
            {
                _combination.Clear();
                _combination.AddRange(_currentEntry);
                _currentEntry.Clear();
            }
        }

        /// <summary>Lock is unlocked now.</summary>
        /// <returns>void</returns>
        private void UnlockedEnter(Object o)
        {
            Console.WriteLine($"UnlockedEnter:{o}");
            HwUnLock();
        }

        /// <summary>Cause an exception to be thrown.</summary>
        /// <returns>void</returns>
        private void ForceFail(Object o)
        {
            Console.WriteLine("ForceFail");
            throw new Exception("ForceFail");
        }
        #endregion
    }
}
