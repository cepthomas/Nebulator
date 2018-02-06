using System;
using System.Collections.Generic;
using Nebulator.Common;


namespace Nebulator.Dynamic
{
    /// <summary>
    /// One sequence definition in the composition.
    /// </summary>
    public class Sequence
    {
        /// <summary>Name used for instantiation.</summary>
        public string Name { get; set; } = Utils.UNKNOWN_STRING;

        /// <summary>List of notes or other elements.</summary>
        public List<SequenceElement> Elements { get; set; } = new List<SequenceElement>();

        /// <summary>Length in ticks.</summary>
        public int Length { get; set; } = 1;

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            List<string> ls = new List<string>
            {
                $"Name:{Name} Length:{Length}"
                //$"Name:{Name}"
            };

            return string.Join(Environment.NewLine, ls);
        }
    }

    /// <summary>
    /// One note or chord or script function or etc in the sequence. Essentially something that gets played.
    /// </summary>
    public class SequenceElement
    {
        #region Properties
        /// <summary>Individual note volume.</summary>
        public int Volume { get; set; } = 90;

        /// <summary>When to play in Sequence.</summary>
        public Time When { get; set; } = new Time() { Tick = 0, Tock = 0 };

        /// <summary>Time between note on/off. 0 (default) means not used.</summary>
        public Time Duration { get; set; } = new Time(0);

        /// <summary>The 0th is the root note and other values comprise possible chord notes.</summary>
        public List<int> Notes { get; private set; } = new List<int>();

        /// <summary>Call a script function.</summary>
        public string Function { get; set; } = "";
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor that parses note string or function name.
        /// </summary>
        /// <param name="s"></param>
        public SequenceElement(string s)
        {
            if(s.Contains("()")) //TODO or get rid of this spec?
            {
                Function = s.Replace("()", "");
                Notes.Clear();
            }
            else
            {
                Notes = NoteUtils.ParseNoteString(s);
            }
        }

        /// <summary>
        /// Constructor from note number.
        /// </summary>
        public SequenceElement(int noteNum)
        {
            Notes.Add(noteNum);
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public SequenceElement(SequenceElement seqel)
        {
            Volume = seqel.Volume;
            Function = seqel.Function;
            When = new Time(seqel.When);
            Duration = new Time(seqel.Duration);
            seqel.Notes.ForEach(n => Notes.Add(n));
        }
        #endregion

        #region Public methods
        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            return $"When:{When} NoteNum:{Notes[0]} Volume:{Volume} Duration:{Duration}";
        }
        #endregion
    }
}
