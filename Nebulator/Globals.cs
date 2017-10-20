using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nebulator.Common;
using Nebulator.Model;
using Nebulator.Engine;


namespace Nebulator
{
    public class Globals
    {
        #region Static definitions
        public const string UNKNOWN_STRING = "???";
        #endregion

        /// <summary>Persisted values for current neb file.</summary>
        public static Persisted CurrentPersisted { get; set; } = new Persisted();

        /// <summary>Variables, controls, etc defined in the script.</summary>
        public static ScriptDynamic Dynamic { get; set; } = new ScriptDynamic();

        /// <summary>Playing the composition.</summary>
        public static bool Playing { get; set; } = false;

        /// <summary>Subdivision setting. Probably should be a multiple of 4.</summary>
        public static int TocksPerTick { get; set; } = 100;//64;//16; TODO

        /// <summary>Current step time clock.</summary>
        public static Time CurrentStepTime = new Time() { Tick = 0, Tock = 0 };

        /// <summary>Midi in/out devices.</summary>
        public static Midi Midi { get; set; } = new Midi();

        /// <summary>Current user settings.</summary>
        public static UserSettings UserSettings { get; set; } = new UserSettings();
    }
}
