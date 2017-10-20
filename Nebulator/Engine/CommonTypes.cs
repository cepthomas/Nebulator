using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nebulator.Common;
using Nebulator.Model;

namespace Nebulator.Engine
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

    /// <summary>
    /// All the compiled script stuff we might need at runtime.
    /// </summary>
    public class ScriptDynamic
    {
        /// <summary>Declared variables.</summary>
        public LazyCollection<Variable> Vars { get; set; } = new LazyCollection<Variable>();

        /// <summary>Midi inputs.</summary>
        public LazyCollection<MidiControlPoint> InputMidis { get; set; } = new LazyCollection<MidiControlPoint>();

        /// <summary>Midi outputs.</summary>
        public LazyCollection<MidiControlPoint> OutputMidis { get; set; } = new LazyCollection<MidiControlPoint>();

        /// <summary>Levers.</summary>
        public LazyCollection<LeverControlPoint> Levers { get; set; } = new LazyCollection<LeverControlPoint>();

        /// <summary>All tracks.</summary>
        public LazyCollection<Track> Tracks { get; set; } = new LazyCollection<Track>();

        /// <summary>All sequences.</summary>
        public LazyCollection<Sequence> Sequences { get; set; } = new LazyCollection<Sequence>();
    }
}
