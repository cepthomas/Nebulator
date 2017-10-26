using System;
using System.Collections.Generic;
using Nebulator.Common;


namespace Nebulator.Model
{
    /// <summary>Track state.</summary>
    public enum TrackState { Normal, Mute, Solo }

    /// <summary>
    /// One melody/chord track - instrument.
    /// </summary>
    public class Track
    {
        /// <summary>The name for this track.</summary>
        public string Name { get; set; } = Globals.UNKNOWN_STRING;

        /// <summary>The midi channel to use: 1 - 16.</summary>
        public int Channel { get; set; } = 1;

        /// <summary>Current volume.</summary>
        public int Volume { get; set; } = 90;

        /// <summary>Humanize factor for volume.</summary>
        public int WobbleVolume { get; set; } = 0;

        /// <summary>Humanize factor for time - before the tock.</summary>
        public int WobbleTimeBefore { get; set; } = 0;

        /// <summary>Humanize factor for time - after the tock.</summary>
        public int WobbleTimeAfter { get; set; } = 0;

        /// <summary>Modulate track notes by +- value.</summary>
        public int Modulate { get; set; } = 0;

        /// <summary>Current state for this track.</summary>
        public TrackState State { get; set; } = TrackState.Normal;

        /// <summary>All the loops for this track.</summary>
        public List<Loop> Loops { get; set; } = new List<Loop>();

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            List<string> ls = new List<string>
            {
                $"Name:{Name} Channel:{Channel} Loops:{Loops.Count}"
            };
            //Loops.ForEach(l => ls.Add($"  Loop:{l}"));
            return string.Join(Environment.NewLine, ls);
        }
    }
}
