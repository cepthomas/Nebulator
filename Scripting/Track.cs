using System;
using System.Collections.Generic;
using Nebulator.Common;


namespace Nebulator.Scripting
{
    /// <summary>Track state.</summary>
    public enum TrackState { Normal, Mute, Solo }

    /// <summary>
    /// One melody/chord track - instrument.
    /// </summary>
    public class Track
    {
        #region Properties
        /// <summary>The name for this track.</summary>
        public string Name { get; set; } = Globals.UNKNOWN_STRING;

        /// <summary>The midi channel to use: 1 - 16.</summary>
        public int Channel { get; set; } = 1;

        /// <summary>Current volume.</summary>
        public int Volume { get; set; } = 90;

        /// <summary>Humanize factor for volume.</summary>
        public int WobbleVolume
        {
            get { return _volWobbler.RangeHigh; }
            set { _volWobbler.RangeHigh = value; }
        }

        /// <summary>Humanize factor for time - before the tock.</summary>
        public int WobbleTimeBefore
        {
            get { return _timeWobbler.RangeLow; }
            set { _timeWobbler.RangeLow = value; }
        }

        /// <summary>Humanize factor for time - after the tock.</summary>
        public int WobbleTimeAfter
        {
            get { return _timeWobbler.RangeHigh; }
            set { _timeWobbler.RangeHigh = value; }
        }

        /// <summary>Modulate track notes by +- value.</summary>
        public int Modulate { get; set; } = 0;

        /// <summary>Current state for this track.</summary>
        public TrackState State { get; set; } = TrackState.Normal;
        #endregion

        #region Fields
        /// <summary>Wobbler for time.</summary>
        Wobbler _timeWobbler = new Wobbler();

        /// <summary>Wobbler for volume.</summary>
        Wobbler _volWobbler = new Wobbler();
        #endregion

        /// <summary>
        /// Get the next time.
        /// </summary>
        /// <returns></returns>
        public int NextTime()
        {
            return _timeWobbler.Next(0);
        }

        /// <summary>
        /// Get the next volume.
        /// </summary>
        /// <param name="def"></param>
        /// <returns></returns>
        public int NextVol(int def)
        {
            return _volWobbler.Next(def);
        }

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            return $"Name:{Name} Channel:{Channel}";
        }
    }
}
