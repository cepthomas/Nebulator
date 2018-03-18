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
    /// All the dynamic script stuff we might want at runtime. Essentially globals - don't let this get too big!
    /// </summary>
    public class DynamicElements
    {
        // DynamicElements refs
        //     Vars - Compiler
        //     InputMidis / OutputMidis / Levers / Sections / Sequences - Compiler, MainForm
        //     Tracks - Compiler, MainForm, NebScript
        //     NoteDefs - Compiler, Sequence, NebScript
        //     MainForm, NebScript, NpScript:
        //         StepTime, Playing, RealTime - Main -> Script
        //         Speed, Volume, FrameRate - Main -> Script -> Main
        //         RuntimeSteps - Script -> Main


        #region Things defined in the script
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
        #endregion

        #region Things shared between host and script at runtime on a per step basis
        /// <summary>Main -> Script</summary>
        public static Time StepTime { get; set; } = new Time();

        /// <summary>Main -> Script</summary>
        public static bool Playing { get; set; } = false;

        /// <summary>Main -> Script</summary>
        public static float RealTime { get; set; } = 0.0f;

        /// <summary>Main -> Script -> Main</summary>
        public static float Speed { get; set; } = 0.0f;

        /// <summary>Main -> Script -> Main</summary>
        public static int Volume { get; set; } = 0;

        /// <summary>Main -> Script -> Main</summary>
        public static int FrameRate { get; set; } = 0;

        /// <summary>Steps added by script functions at runtime e.g. playSequence(). Script -> Main</summary>
        public static StepCollection RuntimeSteps { get; private set; } = new StepCollection();
        #endregion


        /// <summary>Don't even try to do this.</summary>
        DynamicElements() { }

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
            RuntimeSteps.Clear();
        }
    }
}
