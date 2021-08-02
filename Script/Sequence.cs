using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBagOfTricks;
using Nebulator.Common;


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

        /// <summary>Length in beats.</summary>
        public int Beats { get; set; } = 1;
        #endregion

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            return $"NSequence: Beats:{Beats} Elements:{Elements.Count}";
        }
    }

    /// <summary>
    /// Specialized container. Has Add() to support initialization.
    /// </summary>
    public class NSequenceElements : List<NSequenceElement>
    {
        /// <summary>
        /// Like: Z.Add(00.0, "G3", 90, 1.1);
        /// </summary>
        /// <param name="when">Time to play at.</param>
        /// <param name="what">What to play.</param>
        /// <param name="volume">Base volume.</param>
        /// <param name="duration">Time to last. If 0 it's assumed to be a drum and we will supply the note off.</param>
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
        /// Like: Z.Add(00.0, 66, 90, 1.1) or Z.Add(00.0, CrashCymbal1, 90, 1.1);
        /// </summary>
        /// <param name="when">Time to play at.</param>
        /// <param name="what">What to play.</param>
        /// <param name="volume">Base volume.</param>
        /// <param name="duration">Time to last. If 0 it's assumed to be a drum and we will supply the note off.</param>
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
        /// Like: Z.Add(01.0, algoDynamic, 90);
        /// </summary>
        /// <param name="when">Time to play at.</param>
        /// <param name="func">Function to execute.</param>
        /// <param name="volume">Base volume.</param>
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
        /// Like: Z.Add("|5---    8       |7.......7654--- |", "G4.m7", 90);
        /// </summary>
        /// <param name="pattern">Ascii pattern string.</param>
        /// <param name="which">Specific note(s).</param>
        /// <param name="volume">Base volume.</param>
        public void Add(string pattern, string which, double volume)
        {
            foreach (double d in NoteUtils.ParseNoteString(which))
            {
                Add(pattern, d, volume);
            }
        }

        /// <summary>
        /// Like: Z.Add("|5---    8       |7.......7654--- |", 25, BASS_VOL);
        /// </summary>
        /// <param name="pattern">Ascii pattern string.</param>
        /// <param name="which">Specific instrument or drum.</param>
        /// <param name="volume">Volume.</param>
        public void Add(string pattern, double which, double volume)
        {
            // Remove visual markers.
            string s = pattern.Replace("|", "");
            char currentVol = ' '; // default, not sounding
            int start = 0; // index in pattern of start

            void EndCurrent(int index)
            {
                // Make a Note on.
                double volmod = (double)(currentVol - '0') / 10;

                Time dur = new Time((index - start) / Time.SUBDIVS_PER_BEAT, (index - start) % Time.SUBDIVS_PER_BEAT);

                Time when = new Time(start / Time.SUBDIVS_PER_BEAT, start % Time.SUBDIVS_PER_BEAT);
                NSequenceElement ncl = new NSequenceElement(which)
                {
                    When = when,
                    Volume = volume * volmod,
                    Duration = dur
                };

                this.Add(ncl);
            }

            for (int i = 0; i < s.Length; i++)
            {
                switch (s[i])
                {
                    case '-': // continue current note
                        if(currentVol >= '1' && currentVol <= '9')
                        {
                            // ok, do nothing
                        }
                        else
                        {
                            // invalid condition
                            throw new Exception("Invalid \'-\'' in pattern string");
                        }
                        break;

                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        if (currentVol >= '1' && currentVol <= '9')
                        {
                            EndCurrent(i);
                        }

                        // StartNew()
                        currentVol = s[i];
                        start = i;

                        break;

                    case '.': // whitespace
                    case ' ': // whitespace
                        if (currentVol >= '1' && currentVol <= '9')
                        {
                            EndCurrent(i);
                            currentVol = ' ';
                        }
                        break;

                    default:
                        // Invalid char.
                        throw new Exception("Invalid char in pattern string");
                }
            }

            // Stragglers?
            if (currentVol >= '1' && currentVol <= '9')
            {
                EndCurrent(s.Length);
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
        public Time When { get; set; } = new Time();

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
