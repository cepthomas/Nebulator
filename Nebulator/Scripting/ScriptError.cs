using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebulator.Scripting
{
    public enum ScriptErrorType { None, Parse, Compile, Runtime }

    /// <summary>
    /// General error container.
    /// </summary>
    public class ScriptError
    {
        /// <summary>Where it came from.</summary>
        public ScriptErrorType ErrorType { get; set; } = ScriptErrorType.None;

        /// <summary>Original source file.</summary>
        public string SourceFile { get; set; } = Globals.UNKNOWN_STRING;

        /// <summary>Original source line number.</summary>
        public int LineNumber { get; set; } = 0;

        /// <summary>Message from parse or compile or runtime error.</summary>
        public string Message { get; set; } = Globals.UNKNOWN_STRING;

        /// <summary>Readable.</summary>
        public override string ToString() => $"{ErrorType} Error: {SourceFile}({LineNumber}): {Message}";
    }
}
