using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Windows.Forms;
using NLog;
using Nebulator.Common;
using Nebulator.Midi;


namespace Nebulator.Scripting
{
    /// <summary>
    /// Core functions of script. User scripts inherit from this class.
    /// </summary>
    public partial class Script
    {
        #region Properties
        /// <summary>Variables, controls, etc.</summary>
        public Dynamic Dynamic { get; set; } = new Dynamic();

        /// <summary>
        /// The client hosts this control in their UI.
        /// It performs the actual graphics drawing and input.
        /// TODO2 Graphics faster alternative? sharpdx2d, WPF - try drawRecursive() in script.
        /// </summary>
        public UserControl Surface { get; private set; } = null;

        /// <summary>Steps added at runtime.</summary>
        public StepCollection ScriptSteps { get; private set; } = new StepCollection();
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

            /// <summary>Master volume in midi velocity range: 0 - 127. If null means get otherwise set.</summary>
            public int? Volume { get; set; } = null;

            /// <summary>Script can select UI rate in fps. If null means get otherwise set.</summary>
            public int? FrameRate { get; set; } = null;
        }
        public event EventHandler<ScriptEventArgs> ScriptEvent;
        #endregion

        #region Fields
        /// <summary>Script functions that are called from the main nebulator. They are identified by name/key. Typically they are controller input handlers such that the key is the name of the input.</summary>
        protected Dictionary<string, ScriptFunction> _scriptFunctions = new Dictionary<string, ScriptFunction>();
        public delegate void ScriptFunction();
        #endregion




        int[] majorScale = { 0, 2, 4, 5, 7, 9, 11 };

        // TODO1 all this algo stuff:
        //
        // Generative Music becomes Reflective Music when your text can be used as a seed for how it starts.
        // http://spheric-lounge-live-ambient-music.blogspot.com/
        //
        //The random item stream pattern type uses the optional keyword :weight to alter the probability 
        //of an event being selected in relation to the other events in the item stream.
        //need this, for scale note selections.
        //
        //scaleMaj = 0, 2, 4, 5, 7, 9, 11
        //
        //The graph item stream pattern type creates an item stream of user-specified rules for traversing a series 
        //of nodes called a graph.
        // my loops - rules to change/transition.
        //
        // Sonification uses data that is typically not musical and involves remapping this to musical parameters 
        //to create a composition.


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
