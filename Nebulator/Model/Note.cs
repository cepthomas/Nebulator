using System;
using System.Collections.Generic;
using System.Linq;
using Nebulator.Common;


namespace Nebulator.Model
{
    /// <summary>
    /// One note or chord in the loop.
    /// </summary>
    public class Note
    {
        #region Constants
        public const int NOTES_PER_OCTAVE = 12;
        #endregion

        #region Properties
        /// <summary>The chord definitions from ScriptDefinitions.md.</summary>
        public static Dictionary<string, string> ChordDefs { get; set; } = new Dictionary<string, string>();

        /// <summary>White key.</summary>
        public bool Natural
        {
            get
            {
                int[] naturals = { 0, 2, 4, 5, 7, 9, 11 };
                return naturals.Contains(NoteNumber % NOTES_PER_OCTAVE);
            }
        }

        /// <summary>Individual note volume.</summary>
        public int Volume { get; set; } = 90;

        /// <summary>When to play in Sequence.</summary>
        public Time When { get; set; } = new Time() { Tick = 0, Tock = 0 };

        /// <summary>Time between note on/off in Tocks. 0 (default) means not used.</summary>
        public int Duration { get; set; } = 0;

        /// <summary>Convert to/from midi note. Default to middle C == number 60 (0x3C).</summary>
        public int NoteNumber { get; private set; } = 60;

        /// <summary>Additional notes for a chord.</summary>
        public List<Note> ChordNotes { get; private set; } = new List<Note>();
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor that parses note string.
        /// </summary>
        /// <param name="s"></param>
        public Note(string s)
        {
            ParseNoteString(s);
        }

        /// <summary>
        /// Constructor from note ID.
        /// </summary>
        public Note(int notenum)
        {
            NoteNumber = notenum;
            //NoteNumber = Utils.Constrain(notenum, Midi.MIN_MIDI_NOTE, Midi.MAX_MIDI_NOTE);
        }
        #endregion

        #region Public functions
        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            return $"When:{When} NoteNum:{NoteNumber} Volume:{Volume} Duration:{Duration}";
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Parse from input value.
        /// </summary>
        void ParseNoteString(string s)
        {
            // Parse the input value. Many ways to read and fault.
            try
            {
                // Could be:
                // F.4 - named note
                // F.4.dim7 - named chord
                // 57 - numbered
                var parts = s.SplitByToken(".");

                // Start with octave.
                int octave = 4;
                if (parts.Count > 1)
                {
                    octave = int.Parse(parts[1]);
                }

                int notenum = 0;
                switch (parts[0])
                {
                    // Named notes.
                    case "B#": notenum = 0; break;
                    case "C": notenum = 0; break;
                    case "C#": notenum = 1; break;
                    case "Db": notenum = 1; break;
                    case "D": notenum = 2; break;
                    case "D#": notenum = 3; break;
                    case "Eb": notenum = 3; break;
                    case "E": notenum = 4; break;
                    case "Fb": notenum = 4; break;
                    case "E#": notenum = 5; break;
                    case "F": notenum = 5; break;
                    case "F#": notenum = 6; break;
                    case "Gb": notenum = 6; break;
                    case "G": notenum = 7; break;
                    case "G#": notenum = 8; break;
                    case "Ab": notenum = 8; break;
                    case "A": notenum = 9; break;
                    case "A#": notenum = 10; break;
                    case "Bb": notenum = 10; break;
                    case "B": notenum = 11; break;
                    case "Cb": notenum = 11; break;

                    // Numbered notes.
                    case "1": notenum = 0; break;
                    case "2": notenum = 1; break;
                    case "3": notenum = 2; break;
                    case "4": notenum = 3; break;
                    case "5": notenum = 4; break;
                    case "6": notenum = 5; break;
                    case "7": notenum = 6; break;
                    case "8": notenum = 7; break;
                    case "9": notenum = 8; break;
                    case "10": notenum = 9; break;
                    case "11": notenum = 10; break;
                    case "12": notenum = 11; break;

                    default: throw new Exception($"Invalid note:{parts[0]}");
                }

                NoteNumber = notenum + (octave + 1) * NOTES_PER_OCTAVE;

                if (parts.Count > 2)
                {
                    // It's a chord. M, M7, m, m7, etc.
                    var cnotes = ChordDefs[parts[2]];

                    // Add the notes.
                    cnotes.SplitByToken(" ").ForEach(c => ChordNotes.Add(GenIntervalNote(NoteNumber, c)));
                }
            }
            catch (Exception)
            {
                throw new Exception("Invalid note string: " + s);
            }
        }

        /// <summary>
        /// Generate an interval based on this.
        /// </summary>
        /// <param name="rootid"></param>
        /// <param name="sinterval"></param>
        /// <returns></returns>
        Note GenIntervalNote(int rootid, string sinterval)
        {
            int newId = rootid;

            for (int i = 0; i < sinterval.Length; i++)
            {
                char c = sinterval[i]; //bb9

                switch (c)
                {
                    case 'b': newId -= 1; break;
                    case '#': newId += 1; break;

                    default:
                        switch (sinterval.Replace("b", "").Replace("#", ""))
                        {
                            case "1": newId += 0; break;
                            case "2": newId += 2; break;
                            case "3": newId += 4; break;
                            case "4": newId += 5; break;
                            case "5": newId += 7; break;
                            case "6": newId += 8; break;
                            case "7": newId += 10; break;
                            case "9": newId += 14; break;
                            case "11": newId += 17; break;
                            case "13": newId += 20; break;
                        }
                        break;
                }
            }

            Note intervalNote = new Note(newId);

            return intervalNote;
        }
        #endregion
    }
}
