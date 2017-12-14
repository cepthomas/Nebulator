using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nebulator.Common;


namespace Nebulator.Scripting
{
    /// <summary>
    /// All the compiled script stuff we might want at runtime.
    /// </summary>
    public class Dynamic
    {
        /// <summary>Declared variables.</summary>
        public LazyCollection<Variable> Vars { get; set; } = new LazyCollection<Variable>();

        /// <summary>Midi inputs.</summary>
        public LazyCollection<MidiControlPoint> InputMidis { get; set; } = new LazyCollection<MidiControlPoint>();

        /// <summary>Midi outputs.</summary>
        public LazyCollection<MidiControlPoint> OutputMidis { get; set; } = new LazyCollection<MidiControlPoint>();

        /// <summary>Levers.</summary>
        public LazyCollection<LeverControlPoint> Levers { get; set; } = new LazyCollection<LeverControlPoint>();

        /// <summary>All sections.</summary>
        public LazyCollection<Section> Sections { get; set; } = new LazyCollection<Section>();

        /// <summary>All tracks.</summary>
        public LazyCollection<Track> Tracks { get; set; } = new LazyCollection<Track>();

        /// <summary>All sequences.</summary>
        public LazyCollection<Sequence> Sequences { get; set; } = new LazyCollection<Sequence>();
    }
}
