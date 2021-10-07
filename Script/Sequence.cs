using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class Sequence
    {
        #region Properties
        /// <summary>List of notes or other elements.</summary>
        public SequenceElements Elements { get; set; } = new SequenceElements();

        /// <summary>Length in beats.</summary>
        public int Beats { get; set; } = 1;
        #endregion

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            return $"Sequence: Beats:{Beats} Elements:{Elements.Count}";
        }
    }

    /// <summary>
    /// Specialized container. Has Add() to support initialization.
    /// </summary>
    public class SequenceElements : List<SequenceElement>
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
            SequenceElement sel = new(what)
            {
                When = new Time(when),
                Volume = volume,
                Duration = new Time(duration)
            };

            Add(sel);
        }

        /// <summary>
        /// Like: Z.Add(00.0, 66, 90, 1.1) or Z.Add(00.0, CrashCymbal1, 90, 1.1);
        /// </summary>
        /// <param name="when">Time to play at.</param>
        /// <param name="what">What to play.</param>
        /// <param name="volume">Base volume.</param>
        /// <param name="duration">Time to last. If 0 it's assumed to be a drum and we will supply the note off.</param>
        public void Add(double when, DrumDef what, double volume, double duration = 0)
        {
            SequenceElement sel = new((int)what)
            {
                When = new Time(when),
                Volume = volume,
                Duration = new Time(duration)
            };

            Add(sel);
        }

        /// <summary>
        /// Like: Z.Add(01.0, algoDynamic, 90);
        /// </summary>
        /// <param name="when">Time to play at.</param>
        /// <param name="func">Function to execute.</param>
        /// <param name="volume">Base volume.</param>
        public void Add(double when, Action func, double volume)
        {
            SequenceElement sel = new(func)
            {
                When = new Time(when),
                Volume = volume
            };

            Add(sel);
        }

        /// <summary>
        /// Like: Z.Add("|5---    8       |7.......7654--- |", "G4.m7", 90);
        /// </summary>
        /// <param name="pattern">Ascii pattern string.</param>
        /// <param name="resolution">Resolution of chars in the pattern. 4 is quarter notes, etc.</param>
        /// <param name="which">Specific note(s).</param>
        /// <param name="volume">Base volume.</param>
        public void Add(string pattern, int resolution, string which, double volume)//xxx
        {
            foreach (double d in ScriptUtils.GetNotes(which))
            {
                Add(pattern, resolution, d, volume);
            }
        }

        /// <summary>
        /// Like: Z.Add("|5---    8       |7.......7654--- |", RideCymbal1, 90);
        /// </summary>
        /// <param name="pattern">Ascii pattern string.</param>
        /// <param name="resolution">Resolution of chars in the pattern. 4 is quarter notes, etc.</param>
        /// <param name="which">Specific note(s).</param>
        /// <param name="volume">Base volume.</param>
        public void Add(string pattern, int resolution, DrumDef which, double volume)//xxx
        {
            Add(pattern, resolution, (int)which, volume);
        }

        /// <summary>
        /// Like: Z.Add("|5---    8       |7.......7654--- |", 25, BASS_VOL);
        /// </summary>
        /// <param name="pattern">Ascii pattern string.</param>
        /// <param name="resolution">Resolution of chars in the pattern. 4 is quarter notes, etc.</param>
        /// <param name="which">Specific instrument or drum.</param>
        /// <param name="volume">Volume.</param>
        public void Add(string pattern, int resolution, double which, double volume)//xxx
        {
            // Needs to be a multiple of 2 up to 32 (min note for Time.SUBDIVS_PER_BEAT).
            switch(resolution)
            {
                case 1:
                case 2:
                case 4:
                case 8:
                case 16:
                case 32:
                    break;
                default:
                    throw new Exception($"Invalid resolution: {resolution}");
            }

            int scale = Time.SUBDIVS_PER_BEAT * 4 / resolution;

            // Remove visual markers.
            pattern = pattern.Replace("|", "");
            int currentVol = 0; // default, not sounding
            int startIndex = 0; // index in pattern for the start of the current note

            // Local function to package an event.
            void MakeNoteEvent(int index)
            {
                // Make a Note on.
                double volmod = (double)currentVol / 10;

                // Convert to internal resolution.
                int startSubdivs = startIndex * scale;
                int nowSubdivs = index * scale;

                Time dur = new(nowSubdivs - startSubdivs);
                Time when = new(startSubdivs);

                SequenceElement ncl = new(which)
                {
                    When = when,
                    Volume = volume * volmod,
                    Duration = dur
                };

                Add(ncl);
            }

            for (int patternIndex = 0; patternIndex < pattern.Length; patternIndex++)
            {
                switch (pattern[patternIndex])
                {
                    case '-':
                        ///// Continue current note.
                        if(currentVol > 0)
                        {
                            // ok, do nothing
                        }
                        else
                        {
                            // invalid condition
                            throw new Exception("Invalid \'-\'' in pattern string");
                        }
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
                        ///// A new note starting.
                        // Do we need to end the current note?
                        if (currentVol > 0)
                        {
                            MakeNoteEvent(patternIndex);
                        }

                        // Start new note.
                        currentVol = pattern[patternIndex] - '0';
                        startIndex = patternIndex;
                        break;

                    case '.':
                    case ' ':
                        ///// No sound.
                        // Do we need to end the current note?
                        if (currentVol > 0)
                        {
                            MakeNoteEvent(patternIndex);
                        }
                        currentVol = 0;
                        break;

                    default:
                        ///// Invalid char.
                        throw new Exception($"Invalid char in pattern string:{pattern[patternIndex]}");
                }
            }

            // Straggler?
            if (currentVol > 0)
            {
                MakeNoteEvent(pattern.Length);
            }
        }
    }

    /// <summary>
    /// One note or chord or script function etc in the sequence. Essentially something that gets played.
    /// </summary>
    public class SequenceElement
    {
        #region Properties
        /// <summary>Individual note volume.</summary>
        public double Volume { get; set; } = 90;

        /// <summary>When to play in Sequence.</summary>
        public Time When { get; set; } = new Time();

        /// <summary>Time between note on/off. 0 (default) means not used.</summary>
        public Time Duration { get; set; } = new Time(0);

        /// <summary>The 0th is the root note and other values comprise possible chord notes. TODO2 notes below the root.</summary>
        public List<double> Notes { get; private set; } = new List<double>();

        /// <summary>Call a script function.</summary>
        public Action? ScriptFunction { get; set; } = null;
        #endregion

        /// <summary>
        /// Constructor that parses note string.
        /// </summary>
        /// <param name="s"></param>
        public SequenceElement(string s)
        {
            Notes = ScriptUtils.GetNotes(s);
        }

        /// <summary>
        /// Constructor from note number.
        /// </summary>
        public SequenceElement(double noteNum)
        {
            Notes.Add(noteNum);
        }

        ///// <summary>
        ///// Constructor from drum.
        ///// </summary>
        //public SequenceElement(MusicDefinitions.DrumDef drum)
        //{
        //    Notes.Add((double)drum);
        //}

        /// <summary>
        /// Constructor from function.
        /// </summary>
        public SequenceElement(Action func)
        {
            ScriptFunction = func;
            Notes.Clear();
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public SequenceElement(SequenceElement seqel)
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
            return $"SequenceElement: When:{When} NoteNum:{Notes[0]:F2} Volume:{Volume:F2} Duration:{Duration}";
        }
    }
}
