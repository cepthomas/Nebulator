using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBagOfTricks;
using Nebulator.Common;
using Nebulator.Device;


namespace Nebulator.Script
{
    /// <summary>Channel state.</summary>
    public enum ChannelState { Normal, Mute, Solo }

    /// <summary>Display types.</summary>
    public enum DisplayType { LinearMeter, LogMeter, Chart }

    /// <summary>
    /// One bound variable.
    /// </summary>
    public class NVariable
    {
        #region Properties
        /// <summary>Variable name - as shown in ui.</summary>
        public string Name { get; set; } = Utils.UNKNOWN_STRING;

        /// <summary>Value as double. It is initialized from the script supplied value.</summary>
        public double Value
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
                    ValueChangeEvent?.Invoke(this, null);
                }
            }
        }
        double _value;

        /// <summary>Min value - optional.</summary>
        public double Min { get; set; } = 0;

        /// <summary>Max value - optional.</summary>
        public double Max { get; set; } = 100;

        /// <summary>For extra info. Makes me feel dirty.</summary>
        public object Tag { get; set; } = null;
        #endregion

        #region Events
        /// <summary>Notify with new value. This represents a callback defined in a script.</summary>
        public Action Changed;

        /// <summary>Reporting a change to internal listeners.</summary>
        public event EventHandler ValueChangeEvent;
        #endregion

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder($"NVariable: Name:{Name} Value:{Value} Min:{Min} Max:{Max}");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Defines a controller input.
    /// </summary>
    public class NController
    {
        #region Properties
        /// <summary>The associated comm device name.</summary>
        public string DeviceName { get; set; } = Utils.UNKNOWN_STRING;

        /// <summary>The associated comm device.</summary>
        public NInput Device { get; set; } = null;

        /// <summary>The associated numerical (midi) channel to use.</summary>
        public int ChannelNumber { get; set; } = -1;

        /// <summary>The numerical controller type. Usually the same as midi.</summary>
        public int ControllerId { get; set; } = 0;

        /// <summary>The bound var.</summary>
        public NVariable BoundVar { get; set; } = null;
        #endregion

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder($"NController: ControllerId:{ControllerId} BoundVar:{BoundVar.Name} ChannelNumber:{ChannelNumber}");
            return sb.ToString();
        }
    }

    /// <summary>
    /// One channel output, usually an instrument.
    /// </summary>
    public class NChannel
    {
        #region Properties
        /// <summary>The associated device name.</summary>
        public string DeviceName { get; set; } = Utils.UNKNOWN_STRING;

        /// <summary>The associated device.</summary>
        public NOutput Device { get; set; } = null;

        /// <summary>The UI name for this channel.</summary>
        public string Name { get; set; } = Utils.UNKNOWN_STRING;

        /// <summary>The associated numerical (midi) channel to use.</summary>
        public int ChannelNumber { get; set; } = -1;

        /// <summary>Current volume.</summary>
        public double Volume { get; set; } = 90;

        /// <summary>Wobbler for time.</summary>
        public Wobbler TimeWobbler { get; set; } = new Wobbler();

        /// <summary>Wobbler for volume.</summary>
        public Wobbler VolWobbler { get; set; } = new Wobbler();

        /// <summary>Current state for this channel.</summary>
        public ChannelState State { get; set; } = ChannelState.Normal;
        #endregion

        /// <summary>
        /// Get the next time.
        /// </summary>
        /// <returns></returns>
        public int NextTime()
        {
            return (int)TimeWobbler.Next(0);
        }

        /// <summary>
        /// Get the next volume.
        /// </summary>
        /// <param name="def"></param>
        /// <returns></returns>
        public double NextVol(double def)
        {
            return VolWobbler.Next(def);
        }

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            return $"NChannel: Name:{Name} ChannelNumber:{ChannelNumber}";
        }
    }

    /// <summary>
    /// One display output.
    /// </summary>
    public class NDisplay
    {
        #region Properties
        /// <summary>The type of display.</summary>
        public DisplayType DisplayType { get; set; } = DisplayType.LinearMeter;

        /// <summary>The bound var.</summary>
        public NVariable BoundVar { get; set; } = null;
        #endregion

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder($"NDisplay: DisplayType:{DisplayType} BoundVar:{BoundVar.Name}");
            return sb.ToString();
        }
    }

    /// <summary>
    /// One sequence definition.
    /// </summary>
    public class NSequence
    {
        #region Properties
        /// <summary>List of notes or other elements.</summary>
        public List<NSequenceElement> Elements { get; set; } = new List<NSequenceElement>();

        /// <summary>Length in ticks.</summary>
        public int Length { get; set; } = 1;

        /// <summary>Helper to add short default duration to each hit without needing to explicitly put in script.</summary>
        public bool IsDrums { get; set; } = false;
        #endregion

        #region Fields
        /// <summary>Auto noteoff time.</summary>
        const double DRUM_DUR = 0.02;
        #endregion

        /// <summary>
        /// Like: Z.Add(00.00, "G3", 90, 0.60).
        /// </summary>
        /// <param name="when"></param>
        /// <param name="what"></param>
        /// <param name="volume"></param>
        /// <param name="duration"></param>
        public void Add(double when, string what, double volume, double duration = 0)
        {
            // Check for drums.
            duration = duration == 0 && IsDrums ? DRUM_DUR : duration;

            NSequenceElement sel = new NSequenceElement(what) { When = new Time(when), Volume = volume, Duration = new Time(duration) };
            Elements.Add(sel);
        }

        /// <summary>
        /// Like: Z.Add(00.00, 66, 90, 0.60) or Z.Add(00.00, CrashCymbal1, 90, 0.60).
        /// </summary>
        /// <param name="when"></param>
        /// <param name="what"></param>
        /// <param name="volume"></param>
        /// <param name="duration"></param>
        public void Add(double when, int what, double volume, double duration = 0)
        {
            // Check for drums.
            duration = duration == 0 && IsDrums ? DRUM_DUR : duration;

            NSequenceElement sel = new NSequenceElement(what) { When = new Time(when), Volume = volume, Duration = new Time(duration) };
            Elements.Add(sel);
        }

        /// <summary>
        /// Like: Z.Add(01.00, algoDynamic, 90).
        /// </summary>
        /// <param name="when"></param>
        /// <param name="func"></param>
        /// <param name="volume"></param>
        public void Add(double when, Action func, double volume)
        {
            NSequenceElement sel = new NSequenceElement(func) { When = new Time(when), Volume = volume };
            Elements.Add(sel);
        }

        /// <summary>
        /// Like: Z.Add("|x-------x-------|x-------x-------|", AcousticBassDrum, 90).
        /// Each hit is 1/16 note - fixed res for now.
        /// </summary>
        /// <param name="pattern">Ascii pattern string</param>
        /// <param name="which">Specific instrument</param>
        /// <param name="volume">Volume</param>
        /// <param name="duration">Duration</param>
        public void Add(string pattern, int which, double volume, double duration = 0)
        {
            // Check for drums.
            duration = duration == 0 && IsDrums ? DRUM_DUR : duration;

            const int HITS_PER_TICK = 4; // quarter note

            // Remove markers.
            string s = pattern.Replace("|", "");

            for (int i = 0; i < s.Length; i++)
            {
                switch (s[i])
                {
                    case 'x':
                        // Note on.
                        Time t = new Time(i / HITS_PER_TICK, i % HITS_PER_TICK * Time.TOCKS_PER_TICK / HITS_PER_TICK);
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
        public double Volume { get; set; } = 90;

        /// <summary>When to play in Sequence.</summary>
        public Time When { get; set; } = new Time() { Tick = 0, Tock = 0 };

        /// <summary>Time between note on/off. 0 (default) means not used.</summary>
        public Time Duration { get; set; } = new Time(0);

        /// <summary>The 0th is the root note and other values comprise possible chord notes.</summary>
        public List<double> Notes { get; private set; } = new List<double>();

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
        public NSequenceElement(double noteNum)
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
            return $"NSequenceElement: When:{When} NoteNum:{Notes[0]:F2} Volume:{Volume:F2} Duration:{Duration}";
        }
    }
}
