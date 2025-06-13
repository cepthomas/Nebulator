using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;


namespace Ephemera.ScriptCompiler
{
    /// <summary>General script result - error/warn etc.</summary>
    public enum CompileResultType
    {
        Info,       // Not an error.
        Warning,    // Compiler warning.
        Error,      // Compiler error.
        Other       // Custom use.
    }

    /// <summary>General script result container.</summary>
    public class CompileResult
    {
        /// <summary>Where it came from.</summary>
        public CompileResultType ResultType { get; set; } = CompileResultType.Info;

        /// <summary>Original source file.</summary>
        public string SourceFile { get; set; } = "NA";

        /// <summary>Original source line number. -1 means invalid or unknown.</summary>
        public int LineNumber { get; set; } = -1;

        /// <summary>Content.</summary>
        public string Message { get; set; } = "???";

        /// <summary>For humans.</summary>
        public override string ToString()
        {
            return $"{ResultType}:{Message}";
        }
    }

    /// <summary>Parser helper class.</summary>
    public class FileContext
    {
        /// <summary>Current source file.</summary>
        public string SourceFile { get; set; } = "";

        /// <summary>Current source line.</summary>
        public int LineNumber { get; set; } = 1;

        /// <summary>Accumulated script code lines.</summary>
        public List<string> CodeLines { get; set; } = new();
    }
}
