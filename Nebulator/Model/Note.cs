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
        #region Properties
        /// <summary>Individual note volume.</summary>
        public int Volume { get; set; } = 90;

        /// <summary>When to play in Sequence.</summary>
        public Time When { get; set; } = new Time() { Tick = 0, Tock = 0 };

        /// <summary>Time between note on/off. 0 (default) means not used.</summary>
        public Time Duration { get; set; } = new Time(0);

        /// <summary>The 0th is the root note and other values comprise chord notes.</summary>
        public List<int> NoteNumbers { get; private set; } = new List<int>();

        /// <summary>Get the root note. Default to middle C == number 60 (0x3C).</summary>
        public int Root { get { return NoteNumbers.Count > 0 ? NoteNumbers[0] : 60; } }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor that parses note string.
        /// </summary>
        /// <param name="s"></param>
        public Note(string s)
        {
            NoteNumbers = NoteUtils.ParseNoteString(s);
        }

        /// <summary>
        /// Constructor from note ID.
        /// </summary>
        public Note(int noteNum)
        {
            NoteNumbers.Add(noteNum);
        }
        #endregion

        #region Public functions
        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            return $"When:{When} NoteNum:{NoteNumbers[0]} Volume:{Volume} Duration:{Duration}";
        }
        #endregion
    }
}
