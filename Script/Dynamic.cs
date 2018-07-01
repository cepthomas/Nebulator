using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nebulator.Common;
using Nebulator.Protocol;


// A bunch of lightweight classes for runtime elements.

namespace Nebulator.Script
{
    /// <summary>Track state.</summary>
    public enum TrackState { Normal, Mute, Solo }

    /// <summary>
    /// All the dynamic stuff gleaned from the script that we might want at runtime.
    /// </summary>
    public class DynamicElements //TODO make not static?
    {
        #region Properties - things defined in the script that MainForm needs
        /// <summary>Control inputs.</summary>
        public static List<NControlPoint> InputControls { get; set; } = new List<NControlPoint>();

        /// <summary>Control outputs.</summary>
        public static List<NControlPoint> OutputControls { get; set; } = new List<NControlPoint>();

        /// <summary>Levers.</summary>
        public static List<NControlPoint> Levers { get; set; } = new List<NControlPoint>();

        /// <summary>Levers.</summary>
        public static List<NVariable> Variables { get; set; } = new List<NVariable>();

        /// <summary>All sequences.</summary>
        public static List<NSequence> Sequences { get; set; } = new List<NSequence>();

        /// <summary>All sections.</summary>
        public static List<NSection> Sections { get; set; } = new List<NSection>();

        /// <summary>All tracks.</summary>
        public static List<NTrack> Tracks { get; set; } = new List<NTrack>();
        #endregion

        /// <summary>Don't even think about doing this.</summary>
        DynamicElements() { }

        /// <summary>Reset everything.</summary>
        public static void Clear()
        {
            InputControls.Clear();
            OutputControls.Clear();
            Levers.Clear();
            Variables.Clear();
            Sequences.Clear();
            Sections.Clear();
            Tracks.Clear();
        }
    }

    /// <summary>
    /// One instrument.
    /// </summary>
    public class NTrack
    {
        #region Properties
        /// <summary>The UI name for this track.</summary>
        public string Name { get; set; } = Utils.UNKNOWN_STRING;

        /// <summary>The numerical (midi) channel to use: 1 - 16.</summary>
        public int Channel { get; set; } = 1;

        /// <summary>Current volume.</summary>
        public int Volume { get; set; } = 90;

        /// <summary>Humanize factor for volume.</summary>
        public int WobbleVolume
        {
            get { return _volWobbler.RangeHigh; }
            set { _volWobbler.RangeHigh = value; }
        }

        /// <summary>Humanize factor for time - before the tock.</summary>
        public int WobbleTimeBefore
        {
            get { return _timeWobbler.RangeLow; }
            set { _timeWobbler.RangeLow = value; }
        }

        /// <summary>Humanize factor for time - after the tock.</summary>
        public int WobbleTimeAfter
        {
            get { return _timeWobbler.RangeHigh; }
            set { _timeWobbler.RangeHigh = value; }
        }

        /// <summary>Current state for this track.</summary>
        public TrackState State { get; set; } = TrackState.Normal;
        #endregion

        #region Fields
        /// <summary>Wobbler for time.</summary>
        Wobbler _timeWobbler = new Wobbler();

        /// <summary>Wobbler for volume.</summary>
        Wobbler _volWobbler = new Wobbler();
        #endregion

        /// <summary>
        /// Get the next time.
        /// </summary>
        /// <returns></returns>
        public int NextTime()
        {
            return _timeWobbler.Next(0);
        }

        /// <summary>
        /// Get the next volume.
        /// </summary>
        /// <param name="def"></param>
        /// <returns></returns>
        public int NextVol(int def)
        {
            return _volWobbler.Next(def);
        }

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            return $"NTrack: Name:{Name} Channel:{Channel}";
        }
    }

    /// <summary>
    /// One bound variable.
    /// </summary>
    public class NVariable
    {
        #region Properties
        /// <summary>Var name.</summary>
        public string Name { get; set; } = Utils.UNKNOWN_STRING;

        /// <summary>Value as int. It is initialized from the script supplied value.</summary>
        public int Value
        {
            get
            {
                return _value;
            }
            set
            {
                if(value != _value)
                {
                    _value = value;
                    Changed?.Invoke();
                }
            }
        }
        int _value;
        #endregion

        #region Events
        /// <summary>Notify with new value.</summary>
        //public event Action Changed;
        public Action Changed;
        #endregion

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            return $"NVariable: Name:{Name} Value:{Value}";
        }
    }

    /// <summary>
    /// Defines a controller: input/output/pitch/ui/etc control. TODO Support multiple midis and OSC.
    /// </summary>
    public class NControlPoint
    {
        #region Properties
        /// <summary>Associated track - required.</summary>
        public NTrack Track { get; set; } = null;

        /// <summary>The numerical (midi) controller type - required.</summary>
        public int ControllerId { get; set; } = 0;

        /// <summary>The bound var - required.</summary>
        public NVariable BoundVar { get; set; } = null;

        /// <summary>Min value - optional. TODO these?</summary>
        public int Min { get; set; } = -1;

        /// <summary>Max value - optional.</summary>
        public int Max { get; set; } = -1;
        #endregion

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder($"NControlPoint: ControllerId:{ControllerId} BoundVar:{BoundVar} Track:{Track}");

            if (Min != -1)
            {
                sb.Append($" Min:{Min}");
            }

            if (Max != -1)
            {
                sb.Append($" Max:{Max}");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// One top level section.
    /// </summary>
    public class NSection
    {
        #region Properties
        /// <summary>The name for this section.</summary>
        public string Name { get; set; } = Utils.UNKNOWN_STRING;

        /// <summary>Start Tick.</summary>
        public int Start { get; set; } = 0;

        /// <summary>Length in Ticks.</summary>
        public int Length { get; set; } = 0;

        /// <summary>Contained track info.</summary>
        public List<NSectionTrack> SectionTracks { get; set; } = new List<NSectionTrack>();
        #endregion

        /// <summary>
        /// Script callable function.
        /// </summary>
        /// <param name="track"></param>
        /// <param name="seqs"></param>
        public void Add(NTrack track, params NSequence[] seqs)
        {
            SectionTracks.Add(new NSectionTrack() { ParentTrack = track, Sequences = seqs.ToList() });
        }
    }

    /// <summary>
    /// One row in the Section. Describes the sequences associated with a track in the section.
    /// </summary>
    public class NSectionTrack
    {
        #region Properties
        /// <summary>The owner track.</summary>
        public NTrack ParentTrack { get; set; } = null;

        /// <summary>The associated Sequences.</summary>
        public List<NSequence> Sequences { get; set; } = null;
        #endregion
    }

    /// <summary>
    /// One sequence definition in the composition.
    /// </summary>
    public class NSequence
    {
        #region Properties
        /// <summary>List of notes or other elements.</summary>
        public List<NSequenceElement> Elements { get; set; } = new List<NSequenceElement>();

        /// <summary>Length in ticks.</summary>
        public int Length { get; set; } = 1;
        #endregion

        /// <summary>
        /// Z.Add(00.00, "G3", 90, 0.60);
        /// </summary>
        /// <param name="when"></param>
        /// <param name="what"></param>
        /// <param name="volume"></param>
        /// <param name="duration"></param>
        public void Add(double when, string what, int volume, double duration = 0)
        {
            NSequenceElement sel = new NSequenceElement(what) { When = new Time(when), Volume = volume, Duration = new Time(duration) };
            Elements.Add(sel);
        }

        /// <summary>
        /// Z.Add(00.00, 66, 90, 0.60);
        /// Z.Add(00.00, CrashCymbal1, 90, 0.60);
        /// </summary>
        /// <param name="when"></param>
        /// <param name="what"></param>
        /// <param name="volume"></param>
        /// <param name="duration"></param>
        public void Add(double when, int what, int volume, double duration = 0)
        {
            NSequenceElement sel = new NSequenceElement(what) { When = new Time(when), Volume = volume, Duration = new Time(duration) };
            Elements.Add(sel);
        }

        /// <summary>
        /// Z.Add(01.00, algoDynamic, 90);
        /// </summary>
        /// <param name="when"></param>
        /// <param name="func"></param>
        /// <param name="volume"></param>
        public void Add(double when, Action func, int volume)
        {
            NSequenceElement sel = new NSequenceElement(func) { When = new Time(when), Volume = volume };
            Elements.Add(sel);
        }

        /// <summary>
        /// Z.Add("x-------x-------x-------x-------", AcousticBassDrum, 90);
        /// Each hit is 1/16 note - fixed res for now.
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="which"></param>
        /// <param name="volume"></param>
        /// <param name="duration"></param>
        public void Add(string pattern, int which, int volume, double duration = 0)
        {
            const int PATTERN_SIZE = 4; // quarter note

            for (int i = 0; i < pattern.Length; i++)
            {
                switch (pattern[i])
                {
                    case 'x':
                        // Note on.
                        Time t = new Time(i / PATTERN_SIZE, (i % PATTERN_SIZE) * Time.TOCKS_PER_TICK / PATTERN_SIZE);
                        NSequenceElement ncl = new NSequenceElement(which) { When = t, Volume = volume, Duration = new Time(duration) };
                        Elements.Add(ncl);
                        break;

                    case '-':
                        // No note, skip.
                        break;

                    default:
                        // Invalid char.
                        throw null;
                }
            }
        }

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            return $"NSequence: Length:{Length}";
        }
    }

    /// <summary>
    /// One note or chord or script function etc in the sequence. Essentially something that gets played.
    /// </summary>
    public class NSequenceElement
    {
        #region Properties
        /// <summary>Individual note volume.</summary>
        public int Volume { get; set; } = 90;

        /// <summary>When to play in Sequence.</summary>
        public Time When { get; set; } = new Time() { Tick = 0, Tock = 0 };

        /// <summary>Time between note on/off. 0 (default) means not used.</summary>
        public Time Duration { get; set; } = new Time(0);

        /// <summary>The 0th is the root note and other values comprise possible chord notes.</summary>
        public List<int> Notes { get; private set; } = new List<int>();

        /// <summary>Call a script function.</summary>
        public Action ScriptFunction { get; set; } = null;
        #endregion

        /// <summary>
        /// Constructor that parses note string.
        /// </summary>
        /// <param name="s"></param>
        public NSequenceElement(string s)
        {
            Notes = NoteUtils.ParseNoteString(s);
        }

        /// <summary>
        /// Constructor from note number.
        /// </summary>
        public NSequenceElement(int noteNum)
        {
            Notes.Add(noteNum);
        }

        /// <summary>
        /// Constructor from function.
        /// </summary>
        public NSequenceElement(Action func)
        {
            ScriptFunction = func;
            Notes.Clear();
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public NSequenceElement(NSequenceElement seqel)
        {
            Volume = seqel.Volume;
            ScriptFunction = seqel.ScriptFunction;
            When = new Time(seqel.When);
            Duration = new Time(seqel.Duration);
            Notes = seqel.Notes.DeepClone();
        }

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            return $"NSequenceElement: When:{When} NoteNum:{Notes[0]} Volume:{Volume} Duration:{Duration}";
        }
    }
}
