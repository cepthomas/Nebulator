using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nebulator.Common;
using Nebulator.Model;
using Nebulator.Engine;
using Nebulator.Midi;


namespace Nebulator
{
    public class Globals
    {
        /// <summary>General purpose marker.</summary>
        public const string UNKNOWN_STRING = "???";

        /// <summary>Subdivision setting.</summary>
        public const int TOCKS_PER_TICK = 96;

        /// <summary>Persisted values for current neb file.</summary>
        public static Persisted CurrentPersisted { get; set; } = new Persisted();

        /// <summary>Playing the composition.</summary>
        public static bool Playing { get; set; } = false;

        /// <summary>Current step time clock.</summary>
        public static Time CurrentStepTime = new Time() { Tick = 0, Tock = 0 };

        /// <summary>Midi in/out devices.</summary>
        public static MidiInterface MidiInterface { get; set; } = new MidiInterface();

        /// <summary>Current user settings.</summary>
        public static UserSettings UserSettings { get; set; } = new UserSettings();
    }
}
