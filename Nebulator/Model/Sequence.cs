using System;
using System.Collections.Generic;
using Nebulator.Common;


namespace Nebulator.Model
{
    /// <summary>
    /// One sequence definition in the composition.
    /// </summary>
    public class Sequence // FUTURE Trigger one play from controller input, sync to next tick or immediate.
    {
        /// <summary>Name used for instantiation in a loop.</summary>
        public string Name { get; set; } = Globals.UNKNOWN_STRING;

        /// <summary>List of notes.</summary>
        public List<Note> Notes { get; set; } = new List<Note>();

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
            };
            Notes.ForEach(x => ls.Add("  Note " + x.ToString()));
            return string.Join(Environment.NewLine, ls);
        }
    }
}
