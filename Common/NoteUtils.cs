using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MoreLinq;


namespace Nebulator.Common
{
    public class NoteUtils
    {
        #region Constants
        const int NOTES_PER_OCTAVE = 12;
        #endregion

        #region Fields
        /// <summary>Chord and scale definitions added from the script. Value is list of constituent notes.</summary>
        static Dictionary<string, List<string>> _scriptNoteDefs { get; set; } = new Dictionary<string, List<string>>();
        #endregion

        #region Public functions
        /// <summary>
        /// Add a chord or scale definition from the script.
        /// </summary>
        /// <param name="name">"MY_CHORD"</param>
        /// <param name="parts">"1 4 6 b13"</param>
        public static void AddScriptNoteDef(string name, string parts)
        {
            _scriptNoteDefs[name] = parts.SplitByToken(" ");
        }

        /// <summary>
        /// Parse note or notes from input value. Checks both stock items and those defined in the script.
        /// </summary>
        /// <param name="noteString">String to parse.</param>
        /// <returns>List of note numbers - empty if invalid.</returns>
        public static List<int> ParseNoteString(string noteString)
        {
            List<int> notes = new List<int>();

            // Parse the input value.
            try
            {
                // Could be:
                // F4 - named note
                // F4.dim7 - named chord
                // F4.BLA - user defined chord

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
                if (noteNum != null)
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
                    // It's a chord. M, M7, m, m7, etc. Determine the constituents. Start with the stock collection then try user defs.
                    var chordParts = ScriptDefinitions.TheDefinitions.ChordDefs[parts[1]];

                    if(chordParts == null)
                    {
                        chordParts = _scriptNoteDefs[parts[1]];
                    }

                    var chordNotes = chordParts[0].SplitByToken(" ");

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
                        if (iint != null)
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
                //throw new Exception("Invalid note or chord: " + s);
                notes.Clear();
            }

            return notes;
        }

        /// <summary>
        /// Create a list of absolute note numbers for given scale name. Checks both stock items and those defined in the script.
        /// </summary>
        /// <param name="scale">Name of the scale.</param>
        /// <param name="key">Key.octave</param>
        /// <returns>List of scale notes - empty if invalid.</returns>
        public static List<int> GetScaleNotes(string scale, string key)
        {
            var notes = new List<int>();

            // Dig out the root note.
            List<int> keyNotes = ParseNoteString(key);

            if (keyNotes.Count > 0)
            {
                // Start with the stock collection then try user defs.
                var scaleDef = ScriptDefinitions.TheDefinitions.ScaleDefs[scale];

                if (scaleDef == null)
                {
                    scaleDef = _scriptNoteDefs[scale];
                }

                if (scaleDef != null && scaleDef.Count >= 1)
                {
                    // "1 2 b3 #4 5 b6 7"
                   var scaleNotes = scaleDef[0].SplitByToken(" ");

                    scaleNotes.ForEach(sn =>
                    {
                        int? intNum = GetInterval(sn);
                        if (intNum != null)
                        {
                            //noteNum = down ? noteNum - NOTES_PER_OCTAVE : noteNum;
                            notes.Add(keyNotes[0] + intNum.Value);
                        }
                    });
                }
            }

            return notes;
        }

        /// <summary>
        /// Is it a white key?
        /// </summary>
        /// <param name="notenum">Which note</param>
        /// <returns>True/false</returns>
        public static bool IsNatural(int notenum)
        {
            int[] naturals = { 0, 2, 4, 5, 7, 9, 11 };
            return naturals.Contains(SplitNoteNumber(notenum).root % NOTES_PER_OCTAVE);
        }

        /// <summary>
        /// Try to make a note and/or chord string from the param. If it can't find a chord return the individual notes.
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        public static List<string> FormatNotes(List<int> notes)
        {
            List<string> snotes = new List<string>();

            try
            {
                // Dissect root note.
                int rootOctave = SplitNoteNumber(notes[0]).octave;
                int rootNoteNum = SplitNoteNumber(notes[0]).root;

                string sroot = $"\"{NoteNumberToName(rootNoteNum)}{rootOctave}\"";

                foreach (int n in notes)
                {
                    int octave = SplitNoteNumber(n).octave;
                    int root = SplitNoteNumber(n).root;
                    snotes.Add($"\"{NoteNumberToName(root)}{octave}\"");
                }
            }
            catch (Exception)
            {
                throw new Exception($"Invalid note list: {string.Join(",", notes)}");
            }

            return snotes;
        }

        /// <summary>
        /// Convert note number to corresponding drum name.
        /// </summary>
        /// <param name="note"></param>
        /// <returns>The drum name</returns>
        public static string FormatDrum(int note)
        {
            string drumName = note.ToString();

            foreach (string key in ScriptDefinitions.TheDefinitions.DrumDefs.Keys)
            {
                if (string.Join(" ", ScriptDefinitions.TheDefinitions.DrumDefs[key]) == note.ToString())
                {
                    drumName = key;
                    break;
                }
            }

            return drumName;
        }

        /// <summary>
        /// Split a midi note number into root note and octave.
        /// </summary>
        /// <param name="notenum">Absolute note number</param>
        /// <returns>tuple of root and octave</returns>
        public static (int root, int octave) SplitNoteNumber(int notenum)
        {
            int root = notenum % NOTES_PER_OCTAVE;
            int octave = (notenum / NOTES_PER_OCTAVE) - 1;
            return (root, octave);
        }
        #endregion

        #region Conversion functions
        static string[] intervals =
        {
            //"1", "", "2", "", "3", "4", "", "5", "", "6", "", "7",
            //"", "", "9", "", "", "11", "", "", "", "13", "", ""
            "1", "b2", "2", "b3", "3", "4", "b5", "5", "#5", "6", "b7", "7",
            "", "", "9", "#9", "", "11", "#11", "", "", "13", "", ""
        };

        /// <summary>
        /// Get interval offset from name.
        /// </summary>
        /// <param name="sinterval"></param>
        /// <returns>Offset or null if invalid.</returns>
        static int? GetInterval(string sinterval)
        {
            int flats = sinterval.Count(c => c == 'b');
            int sharps = sinterval.Count(c => c == '#');
            sinterval = sinterval.Replace(" ", "").Replace("b", "").Replace("#", "");

            int iinterval = Array.IndexOf(intervals, sinterval);
            return iinterval == -1 ? (int?)null : iinterval + sharps - flats;
        }

        /// <summary>
        /// Get interval name from note number offset.
        /// </summary>
        /// <param name="iint">The name or empty if invalid.</param>
        /// <returns></returns>
        static string GetInterval(int iint)
        {
            return iint >= intervals.Count() ? null : intervals[iint % intervals.Count()];
        }

        /// <summary>
        /// Convert note name into number.
        /// </summary>
        /// <param name="snote">The root of the note without octave.</param>
        /// <returns>The number or null if invalid.</returns>
        static int? NoteNameToNumber(string snote)
        {
            string[] noteNames =
            {
                "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B",
                "B#", "C#", "", "D#", "Fb", "E#", "F#", "", "G#", "", "A#", "Cb",
                "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12"
            };

            int inote = Array.IndexOf(noteNames, snote) % NOTES_PER_OCTAVE;
            return inote == -1 ? null : (int?)inote;
        }

        /// <summary>
        /// Convert note number into name.
        /// </summary>
        /// <param name="inote"></param>
        /// <returns></returns>
        static string NoteNumberToName(int inote)
        {
            int rootNote = SplitNoteNumber(inote).root;
            string[] noteNames = { "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B" };
            return noteNames[rootNote % noteNames.Count()];
        }
        #endregion
    }
}
