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
    /// <summary>
    /// One sequence definition.
    /// </summary>
    public class NSequence
    {
        #region Properties
        /// <summary>List of notes or other elements.</summary>
        public NSequenceElements Elements { get; set; } = new NSequenceElements();

        /// <summary>Readable.</summary>
        public string Name { get; set; } = Utils.UNKNOWN_STRING;

        /// <summary>Length in ticks.</summary>
        public int Length { get; set; } = 1;
        #endregion

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            return $"NSequence: Length:{Length} Name:{Name} Elements:{Elements.Count}";
        }
    }

    /// <summary>
    /// Specialized container.
    /// </summary>
    public class NSequenceElements : List<NSequenceElement>
    {
        /// <summary>
        /// Like: Z.Add(00.00, "G3", 90, 0.60).
        /// </summary>
        /// <param name="when"></param>
        /// <param name="what"></param>
        /// <param name="volume"></param>
        /// <param name="duration"></param>
        public void Add(double when, string what, double volume, double duration = 0)
        {
            NSequenceElement sel = new NSequenceElement(what)
            {
                When = new Time(when),
                Volume = volume,
                Duration = new Time(duration)
            };

            this.Add(sel);
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
            NSequenceElement sel = new NSequenceElement(what)
            {
                When = new Time(when),
                Volume = volume,
                Duration = new Time(duration)
            };

            this.Add(sel);
        }

        /// <summary>
        /// Like: Z.Add(01.00, algoDynamic, 90).
        /// </summary>
        /// <param name="when"></param>
        /// <param name="func"></param>
        /// <param name="volume"></param>
        public void Add(double when, Action func, double volume)
        {
            NSequenceElement sel = new NSequenceElement(func)
            {
                When = new Time(when),
                Volume = volume
            };

            this.Add(sel);
        }

        /// <summary>
        /// Like: Z.Add("|5-------8-------|7-------7-------|", "G4.m7", 90).
        /// Each hit is 1/16 note - fixed res for now.
        /// </summary>
        /// <param name="pattern">Ascii pattern string</param>
        /// <param name="which">Specific note(s)</param>
        /// <param name="volume">Volume</param>
        /// <param name="duration">Duration</param>
        public void Add(string pattern, string which, double volume, double duration)
        {
            foreach(double d in NoteUtils.ParseNoteString(which))
            {
                Add(pattern, d, volume, duration);
            }
        }

        /// <summary>
        /// Like: Z.Add("|x-------x-------|x-------x-------|", AcousticBassDrum, 90).
        /// Each hit is 1/16 note - fixed res for now.
        /// </summary>
        /// <param name="pattern">Ascii pattern string</param>
        /// <param name="which">Specific instrument</param>
        /// <param name="volume">Volume</param>
        /// <param name="duration">Duration</param>
        public void Add(string pattern, double which, double volume, double duration = 0)
        {
            const int HITS_PER_TICK = 4; // aka quarter note TODO1 make adjustable?

            // Remove visual markers.
            string s = pattern.Replace("|", "");

            for (int i = 0; i < s.Length; i++)
            {
                switch (s[i])
                {
                    case '-':
                    case '0':
                        // No note, skip.
                        break;

                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        // Note on.
                        double volmod = (double)(s[i] - '0') / 10;
                        Time t = new Time(i / HITS_PER_TICK, i % HITS_PER_TICK * Time.TOCKS_PER_TICK / HITS_PER_TICK);
                        NSequenceElement ncl = new NSequenceElement(which)
                        {
                            When = t,
                            Volume = volume * volmod,
                            Duration = new Time(duration)
                        };

                        this.Add(ncl);
                        break;

                    default:
                        // Invalid char.
                        throw new Exception("Invalid char in pattern string");
                }
            }
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
