using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nebulator.Common;
using Nebulator.Midi;


namespace Nebulator.Dynamic
{
    // TODO ScriptEntities getting kind of big... combine with RuntimeContext?
    // class ScriptEntities refs
    //     Vars - Compiler
    //     InputMidis/OutputMidis/Levers/Sections/Sequences - Compiler, MainForm
    //     Tracks - Compiler, MainForm, NebScript
    //     NoteDefs - Compiler, Sequence, NebScript
    // 
    // class RuntimeContext refs - MainForm, NebScript
    //     Main -> Script
    //     public Time StepTime
    //     public bool Playing
    //     public float RealTime
    //     Main -> Script -> Main
    //     public float Speed
    //     public int Volume
    //     public int FrameRate
    //     Script -> Main
    //     public StepCollection RuntimeSteps


    /// <summary>
    /// All the dynamic script stuff we might want at runtime. Essentially globals.
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

        /// <summary>The user chord and scale definitions. Value is list of constituent notes.</summary>
        public static LazyCollection<List<string>> NoteDefs { get; set; } = new LazyCollection<List<string>>();

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
            NoteDefs.Clear();
        }
    }
}
