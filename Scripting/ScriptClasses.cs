using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nebulator.Common;
using Nebulator.Midi;

// Misc classes used internally


namespace Nebulator.Scripting
{
    public class ScriptNotImplementedException : Exception
    {
        ScriptNotImplementedException() {  }
        public ScriptNotImplementedException(string function) : base($"Invalid script function: {function}()") { }
    }

    /// <summary>Stuff shared between Main and Script on a per step basis.</summary>
    public class RuntimeValues
    {
        /// <summary>Main -> Script</summary>
        public bool Playing { get; set; }

        /// <summary>Main -> Script</summary>
        public Time StepTime { get; set; }

        /// <summary>Main -> Script</summary>
        public float RealTime { get; set; }

        /// <summary>Main -> Script -> Main</summary>
        public float Speed { get; set; }

        /// <summary>Main -> Script -> Main</summary>
        public int Volume { get; set; }

        /// <summary>Steps added by script functions at runtime e.g. playSequence(). Script -> Main</summary>
        public StepCollection RuntimeSteps { get; private set; } = new StepCollection();

        /// <summary>Script -> Main</summary>
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
}
