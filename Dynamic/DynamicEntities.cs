using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nebulator.Common;


namespace Nebulator.Dynamic
{
    /// <summary>
    /// All the script stuff and general globals we might want at runtime.
    /// </summary>
    public class DynamicEntities
    {
        #region Backing fields
        static bool _playing = false;
        static DateTime _startTime = DateTime.Now;
        #endregion

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

        /// <summary>Playing the part.</summary>
        public static bool Playing
        {
            get { return _playing; }
            set { _playing = value; if (_playing) _startTime = DateTime.Now; }
        }

        /// <summary>Current step time clock.</summary>
        public static Time StepTime { get; set; } = new Time() { Tick = 0, Tock = 0 };

        /// <summary>Seconds since start pressed.</summary>
        public static double RealTime
        {
            get { return (DateTime.Now - _startTime).TotalSeconds; }
        }

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
            Playing = false;
            StepTime = new Time();
        }

        /// <summary>Don't even try to do this.</summary>
        DynamicEntities() { }
    }
}
