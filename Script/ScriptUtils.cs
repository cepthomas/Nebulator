using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NLog;
using NBagOfTricks;
using Nebulator.Common;


namespace Nebulator.Script
{
    /// <summary>Static utilities usable by scripts.</summary>
    public static class ScriptUtils
    {
        #region Constants
        const int NOTES_PER_OCTAVE = 12;
        #endregion

        #region Fields
        /// <summary>My logger - used only for Print() function.</summary>
        static readonly Logger _logger = LogManager.GetLogger("Print");

        /// <summary>Script randomizer.</summary>
        static readonly Random _rand = new();
        #endregion

        #region Note definitions
        static readonly string[] _noteNames =
        {
            "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B",
            "B#", "C#", "", "D#", "Fb", "E#", "F#", "", "G#", "", "A#", "Cb",
            "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12"
        };

        static readonly int[] _naturals =
        {
            0, 2, 4, 5, 7, 9, 11
        };

        static readonly string[] _intervals =
        {
            "1", "b2", "2", "b3", "3", "4", "b5", "5", "#5", "6", "b7", "7",
            "", "", "9", "#9", "", "11", "#11", "", "", "13", "", ""
        };
        #endregion

        #region Public note manipulation functions
        /// <summary>
        /// Add a named chord or scale definition.
        /// </summary>
        /// <param name="name">"MY_CHORD"</param>
        /// <param name="parts">Like "1 4 6 b13"</param>
        public static void CreateNotes(string name, string parts)
        {
            MusicDefinitions.NoteDefs[name] = parts.SplitByToken(" ");
        }

        /// <summary>
        /// Parse note or notes from input value.
        /// </summary>
        /// <param name="noteString">Standard string to parse.</param>
        /// <returns>List of note numbers - empty if invalid.</returns>
        public static List<double> GetNotes(string noteString)
        {
            List<double> notes = new();

            // Parse the input value.
            // Note: Need exception handling here to protect from user script errors.
            try
            {
                // Could be:
                // F4 - named note
                // F4.dim7 - named key/chord
                // F4.FOO - user defined key/chord or scale
                // F4.major - named key/scale

                // Break it up.
                var parts = noteString.SplitByToken(".");
                string snote = parts[0];

                // Start with octave.
                int octave = 4; // default is middle C
                string soct = parts[0].Last().ToString();

                if(soct.IsInteger())
                {
                    octave = int.Parse(soct);
                    snote = snote.Remove(snote.Length - 1);
                }

                // Figure out the root note.
                int? noteNum = NoteNameToNumber(snote);
                if (noteNum is not null)
                {
                    // Transpose octave.
                    noteNum += (octave + 1) * NOTES_PER_OCTAVE;
                }
                else
                {
                    throw new Exception($"Invalid note: {parts[0]}");
                }

                if (parts.Count > 1)
                {
                    // It's a chord. M, M7, m, m7, etc. Determine the constituents.
                    var chordNotes = MusicDefinitions.NoteDefs[parts[1]];
                    //var chordNotes = chordParts[0].SplitByToken(" ");

                    for (int p = 0; p < chordNotes.Count; p++)
                    {
                        string interval = chordNotes[p];
                        bool down = false;

                        if (interval.StartsWith("-"))
                        {
                            down = true;
                            interval = interval.Replace("-", "");
                        }

                        int? iint = GetInterval(interval);
                        if (iint is not null)
                        {
                            iint = down ? iint - NOTES_PER_OCTAVE : iint;
                            notes.Add(noteNum.Value + iint.Value);
                        }
                    }
                }
                else
                {
                    // Just the root.
                    notes.Add(noteNum.Value);
                }
            }
            catch (Exception)
            {
                notes.Clear();
                throw new Exception("Invalid note or chord: " + noteString);
            }

            return notes;
        }
        #endregion

        #region Internal note manipulation functions
        /// <summary>
        /// Is it a white key?
        /// </summary>
        /// <param name="notenum">Which note</param>
        /// <returns>True/false</returns>
        internal static bool IsNatural(int notenum)
        {
            return _naturals.Contains(SplitNoteNumber(notenum).root % NOTES_PER_OCTAVE);
        }

        /// <summary>
        /// Split a midi note number into root note and octave.
        /// </summary>
        /// <param name="notenum">Absolute note number</param>
        /// <returns>tuple of root and octave</returns>
        internal static (int root, int octave) SplitNoteNumber(int notenum)
        {
            int root = notenum % NOTES_PER_OCTAVE;
            int octave = (notenum / NOTES_PER_OCTAVE) - 1;
            return (root, octave);
        }

        /// <summary>
        /// Get interval offset from name.
        /// </summary>
        /// <param name="sinterval"></param>
        /// <returns>Offset or null if invalid.</returns>
        internal static int? GetInterval(string sinterval)
        {
            int flats = sinterval.Count(c => c == 'b');
            int sharps = sinterval.Count(c => c == '#');
            sinterval = sinterval.Replace(" ", "").Replace("b", "").Replace("#", "");

            int iinterval = Array.IndexOf(_intervals, sinterval);
            return iinterval == -1 ? null : iinterval + sharps - flats;
        }

        /// <summary>
        /// Get interval name from note number offset.
        /// </summary>
        /// <param name="iint">The name or empty if invalid.</param>
        /// <returns></returns>
        internal static string? GetInterval(int iint)
        {
            return iint >= _intervals.Length ? null : _intervals[iint % _intervals.Length];
        }

        /// <summary>
        /// Convert note name into number.
        /// </summary>
        /// <param name="snote">The root of the note without octave.</param>
        /// <returns>The number or null if invalid.</returns>
        internal static int? NoteNameToNumber(string snote)
        {
            int inote = Array.IndexOf(_noteNames, snote) % NOTES_PER_OCTAVE;
            return inote == -1 ? null : inote;
        }

        /// <summary>
        /// Try to make a note and/or chord string from the param. If it can't find a chord return the individual notes.
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        internal static List<string> FormatNotes(List<int> notes)
        {
            List<string> snotes = new();

            // Dissect root note.
            //int rootOctave = SplitNoteNumber(notes[0]).octave;
            //int rootNoteNum = SplitNoteNumber(notes[0]).root;
            //string sroot = $"\"{NoteNumberToName(rootNoteNum)}{rootOctave}\"";

            foreach (int n in notes)
            {
                int octave = SplitNoteNumber(n).octave;
                int root = SplitNoteNumber(n).root;
                snotes.Add($"\"{NoteNumberToName(root)}{octave}\"");
            }

            return snotes;
        }

        /// <summary>
        /// Convert note number to corresponding drum name.
        /// </summary>
        /// <param name="note"></param>
        /// <returns>The drum name</returns>
        internal static string FormatDrum(int note)
        {
            var n = Enum.GetName(typeof(DrumDef), note);
            string drumName = n is not null ? n : $"Drum{note}";
            return drumName;
        }

        /// <summary>
        /// Convert note number into name.
        /// </summary>
        /// <param name="inote"></param>
        /// <returns></returns>
        internal static string NoteNumberToName(int inote)
        {
            int rootNote = SplitNoteNumber(inote).root;
            string[] noteNames = { "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B" };
            return noteNames[rootNote % noteNames.Length];
        }
        #endregion

        #region General utilities
        public static double Random(double max)
        {
            return _rand.NextDouble() * max;
        }

        public static double Random(double min, double max)
        {
            return min + _rand.NextDouble() * (max - min);
        }

        public static int Random(int max)
        {
            return _rand.Next(max);
        }

        public static int Random(int min, int max)
        {
            return _rand.Next(min, max);
        }

        public static void Print(params object[] vars)
        {
            _logger.Info(string.Join(", ", vars));
        }
        #endregion
    }
}
