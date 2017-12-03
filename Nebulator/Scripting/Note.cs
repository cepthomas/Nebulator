using System;
using System.Collections.Generic;
using System.Linq;
using Nebulator.Common;
using Nebulator.Midi;


namespace Nebulator.Scripting
{
    /// <summary>
    /// One note or chord in the loop. TODO2 microtonal.
    /// </summary>
    public class Note
    {
        #region Properties
        /// <summary>Individual note volume.</summary>
        public int Volume { get; set; } = 90;

        /// <summary>When to play in Sequence.</summary>
        public Time When { get; set; } = new Time() { Tick = 0, Tock = 0 };

        /// <summary>Time between note on/off. 0 (default) means not used.</summary>
        public Time Duration { get; set; } = new Time(0);

        /// <summary>The 0th is the root note and other values comprise possible chord notes.</summary>
        public List<int> NoteConstituents { get; private set; } = new List<int>();

        /// <summary>Get the root note. Default to middle C == number 60 (0x3C).</summary>
        public int Root { get { return NoteConstituents.Count > 0 ? NoteConstituents[0] : MidiInterface.MIDDLE_C; } }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor that parses note string.
        /// </summary>
        /// <param name="s"></param>
        public Note(string s)
        {
            NoteConstituents = NoteUtils.ParseNoteConstituents(s);
        }

        /// <summary>
        /// Constructor from note ID.
        /// </summary>
        public Note(int noteNum)
        {
            NoteConstituents.Add(noteNum);
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public Note(Note note)
        {
            Volume = note.Volume;
            When = new Time(note.When);
            Duration = new Time(note.Duration);
            note.NoteConstituents.ForEach(n => NoteConstituents.Add(n));
        }
        #endregion

        #region Public methods
        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            return $"When:{When} NoteNum:{NoteConstituents[0]} Volume:{Volume} Duration:{Duration}";
        }
        #endregion
    }
}
