using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Nebulator.Common;
using Nebulator.Midi;


namespace Nebulator
{
    public class Globals
    {
        #region Constants
        /// <summary>General purpose marker.</summary>
        public const string UNKNOWN_STRING = "???";

        /// <summary>Subdivision setting.</summary>
        public const int TOCKS_PER_TICK = 64; // was 96;

        /// <summary>Indicates needs user involvement.</summary>
        public static Color ATTENTION_COLOR = Color.Red;
        #endregion

        /// <summary>Playing the composition.</summary>
        public static bool Playing { get; set; } = false;

        /// <summary>Current step time clock.</summary>
        public static Time CurrentStepTime { get; set; } = new Time() { Tick = 0, Tock = 0 };

        /// <summary>Midi in/out devices.</summary>
        public static MidiInterface MidiInterface { get; set; } = new MidiInterface();

        /// <summary>Current user settings.</summary>
        public static UserSettings UserSettings { get; set; } = new UserSettings();
    }
}
