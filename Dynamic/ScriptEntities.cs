using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nebulator.Common;
using Nebulator.Midi;


namespace Nebulator.Dynamic
{
    /// <summary>
    /// All the dynamic script stuff we might want at runtime.
    /// </summary>
    public class ScriptEntities
    {
        /// <summary>Declared variables.</summary>
        public static LazyCollection<Variable> Vars { get; set; } = new LazyCollection<Variable>();

        /// <summary>Midi inputs.</summary>
        public static LazyCollection<MidiControlPoint> InputMidis { get; set; } = new LazyCollection<MidiControlPoint>();

        /// <summary>Midi outputs.</summary>
        public static LazyCollection<MidiControlPoint> OutputMidis { get; set; } = new LazyCollection<MidiControlPoint>();

        /// <summary>Levers.</summary>
        public static LazyCollection<LeverControlPoint> Levers { get; set; } = new LazyCollection<LeverControlPoint>();

        /// <summary>All sections.</summary>
        public static LazyCollection<Section> Sections { get; set; } = new LazyCollection<Section>();

        /// <summary>All tracks.</summary>
        public static LazyCollection<Track> Tracks { get; set; } = new LazyCollection<Track>();

        /// <summary>All sequences.</summary>
        public static LazyCollection<Sequence> Sequences { get; set; } = new LazyCollection<Sequence>();

        /// <summary>Don't even try to do this.</summary>
        ScriptEntities() { }

        /// <summary>Reset everything.</summary>
        public static void Clear()
        {
            Vars.Clear();
            InputMidis.Clear();
            OutputMidis.Clear();
            Levers.Clear();
            Sections.Clear();
            Tracks.Clear();
            Sequences.Clear();
        }
    }
}
