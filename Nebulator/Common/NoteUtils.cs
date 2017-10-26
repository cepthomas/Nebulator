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
        public const int NOTES_PER_OCTAVE = 12;

        /// <summary>Note numbers that are white keys.</summary>
        static int[] _naturals = { 0, 2, 4, 5, 7, 9, 11 };

        /// <summary>Interval names. Index is the relative note number. Empty is an invalid value.</summary>
        static string[] _intervals = { "1", "b2", "2", "b3", "3", "4", "b5", "5", "#5", "6", "b7", "7", "", "", "9", "#9", "", "11", "#11", "", "", "13" };

        /// <summary>Note names. Index is the relative note number.</summary>
        static string[] _noteNames = { "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B" };

        /// <summary>Alternate note names. Index is the relative note number.</summary>
        static string[] _noteNamesAlt = { "B#", "C#", "", "D#", "Fb", "E#", "F#", "", "G#", "", "A#", "Cb" };

        /// <summary>Notes using numbers. Index is the relative note number.</summary>
        static string[] _noteNamesNumbered = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" };

        /// <summary>The chord definitions from ScriptDefinitions.md and user settings. Key is string of constituent notes, Value is chord name.</summary>
        static Dictionary<string, string> _chordDefsByNotes = new Dictionary<string, string>();

        /// <summary>The chord definitions from ScriptDefinitions.md and user settings. Key is string of constituent notes, Value is chord name.</summary>
        static Dictionary<string, string> _chordDefsByName = new Dictionary<string, string>();

        /// <summary>The midi drum definitions from ScriptDefinitions.md. Key is note num, value is midi drum name. FUTURE a bit ungainly...</summary>
        static Dictionary<string, string> _drumDefsByNote = new Dictionary<string, string>();

        /// <summary>
        /// Initialize the note and chord helpers.
        /// </summary>
        public static void Init()
        {
            _chordDefsByName.Clear();
            _chordDefsByNotes.Clear();
            _drumDefsByNote.Clear();

            // Read the file.
            Dictionary<string, string> section = null;

            foreach (string sl in File.ReadAllLines(@"Resources\ScriptDefinitions.md"))
            {
                List<string> parts = sl.SplitByToken("|");

                if (parts.Count > 1)
                {
                    switch (parts[0])
                    {
                        case "Chord":
                            section = _chordDefsByNotes;
                            break;

                        case "Drum":
                            section = _drumDefsByNote;
                            break;

                        case string s when !s.StartsWith("---"):
                            section?.Add(string.Join(":", parts[1].SplitByTokens(" ")), parts[0]);
                            break;
                    }
                }
                else
                {
                    section = null;
                }
            }

            // Add user chords.
            foreach (string s in Globals.UserSettings.Chords)
            {
                List<string> parts = s.SplitByToken(":");
                if (parts.Count >= 2)
                {
                    _chordDefsByNotes.Add(string.Join(":", parts[1].SplitByTokens(" ")), parts[0]);
                }
            }

            // Create the flip-flop of chords.
            foreach (KeyValuePair<string, string> kv in _chordDefsByNotes)
            {
                _chordDefsByName.Add(kv.Value, kv.Key);
            }
        }

        /// <summary>
        /// Parse from input value.
        /// </summary>
        public static List<int> ParseNoteString(string s)
        {
            List<int> notes = new List<int>();

            // Parse the input value. Many ways to read and fault.
            try
            {
                // Could be:
                // F.4 - named note
                // F.4.dim7 - named chord
                // 57 - numbered note
                // LowTom - named drum

                var parts = s.SplitByToken(".");

                // Start with octave.
                int octave = 4; // default is middle C
                if (parts.Count > 1)
                {
                    octave = int.Parse(parts[1]);
                }

                // Figure out the root note.
                int rootNote = -1;

                for (int i = 0; i < NOTES_PER_OCTAVE; i++)
                {
                    if(parts[0] == _noteNames[i] || parts[0] == _noteNamesAlt[i] || parts[0] == _noteNamesNumbered[i])
                    {
                        rootNote = i;
                        break;
                    }
                }

                if(rootNote == -1)
                {
                    throw new Exception($"Invalid note:{parts[0]}");
                }

                // Transpose octave.
                rootNote += (octave + 1) * NOTES_PER_OCTAVE;

                if (parts.Count > 2)
                {
                    // It's a chord. M, M7, m, m7, etc. Determine the constituents.
                    List<string> chordParts = _chordDefsByName[parts[2]].SplitByToken(":");

                    for (int p = 0; p < chordParts.Count; p++)
                    {
                        string interval = chordParts[p];
                        int noteNum = -1;
                        bool down = false;

                        if(interval.StartsWith("-"))
                        {
                            down = true;
                            interval = interval.Replace("-", "");
                        }

                        for (int i = 0; i < _intervals.Count(); i++)
                        {

                            if (interval == _intervals[i])
                            {
                                noteNum = down ? i - NOTES_PER_OCTAVE : i;
                                break;
                            }
                        }

                        if (noteNum != -1)
                        {
                            notes.Add(rootNote + noteNum);
                        }
                    }
                }
                else
                {
                    // Just the root.
                    notes.Add(rootNote);
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
            return _naturals.Contains(SplitNoteNumber(notenum).root);
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

                string sroot = $"{_noteNames[rootNoteNum]}.{rootOctave}";

                if (notes.Count > 1)
                {
                    // It's a chord. M, M7, m, m7, etc.
                    // Find a match in our internal list.
                    List<string> intervals = new List<string>();

                    foreach(int i in notes)
                    {
                        intervals.Add(_intervals[i - notes.Min()]);
                    }

                    string s = string.Join(":", intervals);

                    if(_chordDefsByNotes.ContainsKey(s))
                    {
                        // Known chord.
                        snotes.Add($"{sroot}.{_chordDefsByNotes[s]}");
                    }
                    else
                    {
                        // Unknown - place marker in file for user to edit.
                        string schord = "UNKNOWN_CHORD";
                        foreach (int n in notes)
                        {
                            int octave = SplitNoteNumber(n).octave;
                            int root = SplitNoteNumber(n).root;
                            schord += $"_{_noteNames[root]}.{octave}";
                        }
                        snotes.Add(schord);

                        // Or do this?
                        //// Unknown - add components individually.
                        //foreach (int n in notes)
                        //{
                        //    int octave = SplitNoteNumber(n).octave;
                        //    int root = SplitNoteNumber(n).root;
                        //    snotes.Add($"{_noteNames[root]}.{octave}");
                        //}
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
            _drumDefsByNote.TryGetValue(note.ToString(), value: out drumName);
            return drumName;
        }

        /// <summary>
        /// Split a midi note number into root note and octave.
        /// </summary>
        /// <param name="val"></param>
        /// <returns>tuple of root and octave</returns>
        public static (int root, int octave) SplitNoteNumber(int val)
        {
            int root = val % NOTES_PER_OCTAVE;
            int octave = (val / NOTES_PER_OCTAVE) - 1;
            return (root, octave);
        }
    }
}
