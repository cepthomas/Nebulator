using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using Nebulator.Common;
using Nebulator.Midi;
using Nebulator.Dynamic;

// TODO2 Thread for script execution step()/draw()? EventLoopScheduler, async / await keywords and the Task Parallel Library

namespace Nebulator.Scripting
{
    /// <summary>Stuff shared between Main and Script on a per step basis.</summary>
    public class RuntimeValues
    {
        /// <summary>Main > Script</summary>
        public bool Playing { get; set; }

        /// <summary>Main > Script</summary>
        public Time StepTime { get; set; }

        /// <summary>Main > Script</summary>
        public float RealTime { get; set; }

        /// <summary>Main > Script > Main</summary>
        public float Speed { get; set; }

        /// <summary>Main > Script > Main</summary>
        public int Volume { get; set; }

        /// <summary>Steps added by script functions at runtime e.g. playSequence(). Script > Main</summary>
        public StepCollection RuntimeSteps { get; private set; } = new StepCollection();

        /// <summary>Script > Main</summary>
        public List<string> PrintLines { get; private set; } = new List<string>();
    }

    /// <summary>
    /// General error container.
    /// </summary>
    public class ScriptError
    {
        public enum ScriptErrorType { None, Parse, Compile, Runtime }

        /// <summary>Where it came from.</summary>
        public ScriptErrorType ErrorType { get; set; } = ScriptErrorType.None;

        /// <summary>Original source file.</summary>
        public string SourceFile { get; set; } = Definitions.UNKNOWN_STRING;

        /// <summary>Original source line number.</summary>
        public int LineNumber { get; set; } = 0;

        /// <summary>Message from parse or compile or runtime error.</summary>
        public string Message { get; set; } = Definitions.UNKNOWN_STRING;

        /// <summary>Readable.</summary>
        public override string ToString() => $"{ErrorType} Error: {SourceFile}({LineNumber}): {Message}";
    }


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
