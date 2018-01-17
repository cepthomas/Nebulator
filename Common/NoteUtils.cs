using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace Nebulator.Common
{
    public class NoteUtils
    {
        #region Fields
        const int NOTES_PER_OCTAVE = 12;
        const string UNKNOWN_CHORD = "???";

        /// <summary>The chord definitions from ScriptDefinitions.md and user settings. Key is chord name, Value is string of constituent notes.</summary>
        static Dictionary<string, string> _chordDefs = new Dictionary<string, string>();

        /// <summary>The scale definitions from ScriptDefinitions.md. Key is scale name, Value is string of constituent notes.</summary>
        static Dictionary<string, string> _scaleDefs = new Dictionary<string, string>();

        /// <summary>The midi drum definitions from ScriptDefinitions.md. Key is midi drum name, value is note num.</summary>
        static Dictionary<string, string> _drumDefs = new Dictionary<string, string>();
        #endregion

        #region Public functions
        /// <summary>
        /// Initialize the note and chord helpers.
        /// </summary>
        public static void Init()
        {
            _chordDefs.Clear();
            _scaleDefs.Clear();
            _drumDefs.Clear();

            ///// Read the defs file.
            Dictionary<string, string> section = null;
            // Chord:
            // M | 1 3 5 | Named after the major 3rd interval between root and 3.
            // Scale:
            // Acoustic | 1 2 3 #4 5 6 b7 | Acoustic scale | whole tone | minor
            // Drum:
            // AcousticBassDrum | 35

            foreach (string sl in File.ReadAllLines(@"Resources\ScriptDefinitions.md"))
            {
                List<string> parts = sl.SplitByToken("|");

                if (parts.Count > 1)
                {
                    switch (parts[0])
                    {
                        case "Chord":
                            section = _chordDefs;
                            break;

                        case "Scale":
                            section = _scaleDefs;
                            break;

                        case "Drum":
                            section = _drumDefs;
                            break;

                        case string s when !s.StartsWith("---"):
                            section?.Add(parts[0], parts[1]);
                            break;
                    }
                }
                else
                {
                    section = null;
                }
            }

            ///// Add user chords and scales.
            foreach (string s in Globals.TheSettings.Chords)
            {
                List<string> parts = s.SplitByToken(" ");
                if (parts.Count >= 2)
                {
                    _chordDefs.Add(parts[0], string.Join(" ", parts.GetRange(1, parts.Count - 1)));
                }
            }

            // Add marker for parsed values.
            _chordDefs.Add(UNKNOWN_CHORD, "");

            foreach (string s in Globals.TheSettings.Scales)
            {
                List<string> parts = s.SplitByToken(" ");
                if (parts.Count >= 2)
                {
                    _scaleDefs.Add(parts[0], string.Join(" ", parts.GetRange(1, parts.Count - 1)));
                }
            }
        }

        /// <summary>
        /// Parse from input value.
        /// </summary>
        public static List<int> ParseNoteString(string s)
        {
            List<int> notes = new List<int>();

            // Parse the input value.
            try
            {
                // Could be:
                // F4 - named note
                // F4.dim7 - named chord

                // Break it up.
                var parts = s.SplitByToken(".");
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
                    throw new Exception($"Invalid note:{parts[0]}");
                }

                if (parts.Count > 1)
                {
                    // It's a chord. M, M7, m, m7, etc. Determine the constituents.
                    List<string> chordParts = _chordDefs[parts[1]].SplitByToken(" ");

                    for (int p = 0; p < chordParts.Count; p++)
                    {
                        string interval = chordParts[p];
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
                throw new Exception("Invalid note or chord: " + s);
            }

            return notes;
        }

        /// <summary>White key?</summary>
        public static bool IsNatural(int notenum)
        {
            int[] naturals = { 0, 2, 4, 5, 7, 9, 11 };
            return naturals.Contains(SplitNoteNumber(notenum).root % 12);
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

                string sroot = $"{NoteNumberToName(rootNoteNum)}.{rootOctave}";

                if (notes.Count > 1)
                {
                    // It's a chord. M, M7, m, m7, etc.
                    // Find a match in our internal list.
                    List<string> intervals = new List<string>();

                    foreach(int i in notes)
                    {
                        intervals.Add(GetInterval(i - notes.Min()));
                    }

                    string s = string.Join(" ", intervals);
                    string chord = null;
                    foreach(KeyValuePair<string, string> kv in _chordDefs)
                    {
                        if(kv.Value == s)
                        {
                            chord = kv.Key;
                            break;
                        }
                    }

                    if(chord != null)
                    {
                        // Known chord.
                        snotes.Add($"{sroot}.{chord}");
                    }
                    else
                    {
                        // Unknown - add components individually.
                        foreach (int n in notes)
                        {
                            int octave = SplitNoteNumber(n).octave;
                            int root = SplitNoteNumber(n).root;
                            snotes.Add($"{NoteNumberToName(root)}.{octave}.{UNKNOWN_CHORD}");
                        }
                    }
                }
                else
                {
                    // Just the root.
                    snotes.Add(sroot);
                }
            }
            catch (Exception)
            {
                throw new Exception($"Invalid note list:{string.Join(",", notes)}");
            }

            return snotes;
        }

        /// <summary>
        /// Convert note number to corresponding drum name.
        /// </summary>
        /// <param name="note"></param>
        /// <returns></returns>
        public static string FormatDrum(int note)
        {
            string drumName = Globals.UNKNOWN_STRING;

            foreach (KeyValuePair<string, string> kv in _drumDefs)
            {
                if (kv.Value == note.ToString())
                {
                    drumName = kv.Key;
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

        /// <summary>
        /// Create a list of absolute note numbers for given scale name.
        /// </summary>
        /// <param name="scale">Name of the scale.</param>
        /// <param name="key">Key.octave</param>
        /// <returns>List of scale notes or null if invalid.</returns>
        public static List<int> GetScaleNotes(string scale, string key)
        {
            List<int> notes = null;

            List<int> keyNotes = ParseNoteString(key);

            if (_scaleDefs.ContainsKey(scale) && keyNotes.Count > 0)
            {
                notes = new List<int>();

                // "1 2 b3 #4 5 b6 7"

                _scaleDefs[scale].SplitByToken(" ").ForEach(si =>
                {
                    int? intNum = GetInterval(si);
                    if (intNum != null)
                    {
                        //noteNum = down ? noteNum - NOTES_PER_OCTAVE : noteNum;
                        notes.Add(keyNotes[0] + intNum.Value);
                    }
                });
            }

            return notes;
        }
        #endregion

        #region Conversion functions
        /// <summary>
        /// Get interval offset from name.
        /// </summary>
        /// <param name="sinterval"></param>
        /// <returns>Offset or null if invalid.</returns>
        static int? GetInterval(string sinterval)
        {
            string[] intervals =
            {
                "1", "", "2", "", "3", "4", "", "5", "", "6", "", "7",
                "", "", "9", "", "", "11", "", "", "", "13", "", ""
            };

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
            string[] intervals =
            {
                "1", "b2", "2", "b3", "3", "4", "b5", "5", "#5", "6", "b7", "7",
                "", "", "9", "#9", "", "11", "#11", "", "", "13", "", ""
            };
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
