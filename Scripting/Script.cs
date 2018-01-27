using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using Nebulator.Common;
using Nebulator.Midi;
using Nebulator.Dynamic;


namespace Nebulator.Scripting
{

    /// <summary>
    /// Core functions of script. User scripts inherit from this class.
    /// </summary>
    public partial class Script
    {
        #region Properties
        /// <summary>Current working set of dynamic values - things shared between host and script.</summary>
        public RuntimeValues RtVals { get; set; } = new RuntimeValues();
        #endregion

        #region Fields
        /// <summary>Script randomizer.</summary>
        Random _rand = new Random();

        /// <summary>Current font to draw.</summary>
        Font _font = new Font("Arial", 12f, GraphicsUnit.Pixel);

        /// <summary>Current pen to draw.</summary>
        Pen _pen = new Pen(Color.Black, 1f) { LineJoin = LineJoin.Round, EndCap = LineCap.Round, StartCap = LineCap.Round };

        /// <summary>Current brush to draw.</summary>
        SolidBrush _brush = new SolidBrush(Color.Transparent);

        /// <summary>Current text alignment.</summary>
        int _xAlign = LEFT;

        /// <summary>Current text alignment.</summary>
        int _yAlign = BASELINE;

        /// <summary>General purpose stack</summary>
        Stack<object> _matrixStack = new Stack<object>();

        /// <summary>Where to keep style stack.</summary>
        Bag _style = new Bag();

        /// <summary>Background color. Internal so ScriptSurface can access.</summary>
        internal Color _bgColor = Color.LightGray;

        /// <summary>Smoothing option. Internal so ScriptSurface can access.</summary>
        internal bool _smooth = true;

        /// <summary>Loop option. Internal so ScriptSurface can access.</summary>
        internal bool _loop = true;

        /// <summary>Redraw option. Internal so ScriptSurface can access.</summary>
        internal bool _redraw = false;

        /// <summary>Current working Graphics object to draw on. Internal so ScriptSurface can access</summary>
        internal Graphics _gr = null;

        /// <summary>
        /// Script functions that are called from the main nebulator. They are identified by name/key.
        /// Typically they are controller input handlers such that the key is the name of the input.
        /// </summary>
        protected Dictionary<string, ScriptFunction> _scriptFunctions = new Dictionary<string, ScriptFunction>();
        public delegate void ScriptFunction();

        /// <summary>
        /// Reference to current script so nested classes have access to it.
        /// Processing uses java which would not require this hack.
        /// </summary>
        protected static Script s;
        #endregion

        #region Internal functions
        /// <summary>
        /// Base constructor provides internal access to the script.
        /// </summary>
        protected internal Script()
        {
            s = this;
        }

        /// <summary>
        /// Execute a script function. No error checking, presumably the compiler did that. Caller will have to deal with any runtime exceptions.
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
