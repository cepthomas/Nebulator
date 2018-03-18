using System;
using System.Collections.Generic;


namespace Nebulator.Dynamic
{
    /// <summary>
    /// Defines an input or output midi control. TODO Support multiple midis, OSC.
    /// </summary>
    public class MidiControlPoint
    {
        #region Properties
        /// <summary>The bound var.</summary>
        public Variable RefVar { get; set; } = null;

        /// <summary>Midi channel.</summary>
        public int Channel { get; set; } = -1;

        /// <summary>The midi controller type.</summary>
        public int MidiController { get; set; } = -1;
        #endregion

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            return $"MidiControlPoint: RefVar:{RefVar} Channel:{Channel} MidiController:{MidiController}";
        }
    }

    /// <summary>
    /// Defines an input lever.
    /// </summary>
    public class LeverControlPoint
    {
        #region Properties
        /// <summary>The bound var.</summary>
        public Variable RefVar { get; set; } = null;

        /// <summary>Min value.</summary>
        public int Min { get; set; } = 0;

        /// <summary>Max value.</summary>
        public int Max { get; set; } = 0;
        #endregion

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            return $"LeverControlPoint: RefVar:{RefVar}";
        }
    }
}
