using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using Nebulator.Common;
using Nebulator.Midi;
using Nebulator.FastTimer;



namespace Nebulator.Scripting
{
    /// <summary>
    /// Core functions of script. User scripts inherit from this class.
    /// </summary>
    public partial class Script
    {
        #region Properties
        /// <summary>The client hosts this control in their UI. It performs the actual graphics drawing and input. </summary>
        public UserControl Surface { get; private set; } = null;

        /// <summary>All the defined variables, controls, etc for use at runtime.</summary>
        public Dynamic Dynamic { get; set; } = new Dynamic();

        /// <summary>Steps added at runtime.</summary>
        public StepCollection RuntimeSteps { get; private set; } = new StepCollection();
        #endregion

        #region Events
        /// <summary>
        /// Interaction with the host. TODO2 A bit klunky, could be improved. Separate events for each?
        /// </summary>
        public class ScriptEventArgs : EventArgs
        {
            /// <summary>If not null, print this.</summary>
            public string Message { get; set; } = null;

            /// <summary>Master speed in bpm. If null means get otherwise set.</summary>
            public double? Speed { get; set; } = null;

            /// <summary>Master volume. If null means get otherwise set.</summary>
            public int? Volume { get; set; } = null;

            /// <summary>Script can select UI rate in fps. If null means get otherwise set.</summary>
            public int? FrameRate { get; set; } = null;
        }
        public event EventHandler<ScriptEventArgs> ScriptEvent;
        #endregion

        #region Fields
        /// <summary>Script randomizer.</summary>
        Random _rand = new Random();

        /// <summary>Diagnostics for UI execution time.</summary>
        TimingAnalyzer _tanUi = new TimingAnalyzer() { SampleSize = 50 };

        /// <summary>Script functions that are called from the main nebulator. They are identified by name/key. Typically they are controller input handlers such that the key is the name of the input.</summary>
        protected Dictionary<string, ScriptFunction> _scriptFunctions = new Dictionary<string, ScriptFunction>();
        public delegate void ScriptFunction();
        #endregion

        #region Internal overhead
        /// <summary>
        /// Constructor called by derived scripts.
        /// </summary>
        protected Script()
        {
            CreateSurface();
        }

        /// <summary>
        /// Execute a script function. No error checking, presumably the compiler did that.
        /// </summary>
        /// <param name="which"></param>
        public void ExecScriptFunction(string which)
        {
            if(_scriptFunctions.ContainsKey(which))
            {
                _scriptFunctions[which].Invoke();
            }
        }
        #endregion
    }
}
