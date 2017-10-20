using System;
using System.Collections.Generic;
using Nebulator.Common;


namespace Nebulator.Model
{
    /// <summary>
    /// One play of a Sequence.
    /// </summary>
    public class Loop
    {
        /// <summary>NoteSequence.Name</summary>
        public string SequenceName { get; set; } = Globals.UNKNOWN_STRING;

        /// <summary>Which tick to play at.</summary>
        public int StartTick { get; set; } = 0;

        /// <summary>Which tick to play to - non-inclusive</summary>
        public int EndTick { get; set; } = 0;

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            return $"When:{StartTick} Until:{EndTick} SequenceName:{SequenceName}";
        }
    }
}
