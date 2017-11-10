using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Windows.Forms;
using NLog;
using Nebulator.Common;


namespace Nebulator.Scripting
{
    /// <summary>
    /// Core functions of script.
    /// Nebulator user script classes must inherit from this class.
    /// FUTURE Would like multiple inheritance so not everything is part of one giant class. Don't really want to start creating a bunch of interfaces.
    /// </summary>
    public partial class Script
    {
        #region Properties
        /// <summary>Variables, controls, etc.</summary>
        public Dynamic Dynamic { get; set; } = new Dynamic();

        /// <summary>
        /// The client hosts this control in their UI.
        /// It performs the actual graphics drawing and input.
        /// FUTURE Graphics alternative? sharpdx2d, wpf, uwp.
        /// </summary>
        public UserControl Surface { get; private set; } = null;
        #endregion

        #region Events
        /// <summary>Interaction with the host.</summary>
        public event EventHandler<ScriptEventArgs> ScriptEvent;

        public class ScriptEventArgs : EventArgs
        {
            /// <summary>Something to print if not null.</summary>
            public string Message { get; set; } = null;
            /// <summary>Get master speed.</summary>
            public double Speed { get; set; } = 80.0;
            /// <summary>Get master volume.</summary>
            public int Volume { get; set; } = 100;
        }
        #endregion

        #region Fields
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
