using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Nebulator.Common;


namespace Nebulator.Common
{
    public class Globals // TODO0 - all of these somewhere else?
    {
        #region Constants
        /// <summary>General purpose marker.</summary>
        public const string UNKNOWN_STRING = "???";

        /// <summary>Subdivision setting.</summary>
        public const int TOCKS_PER_TICK = 96;

        /// <summary>Indicates needs user involvement.</summary>
        public static Color ATTENTION_COLOR = Color.Red;
        #endregion

        #region Fields
        static bool _playing = false;
        static DateTime _startTime = DateTime.Now;
        #endregion

        /// <summary>Playing the composition.</summary>
        public static bool Playing
        {
            get { return _playing; }
            set { _playing = value; if(_playing) _startTime = DateTime.Now; }
        }

        /// <summary>Current step time clock.</summary>
        public static Time StepTime { get; set; } = new Time() { Tick = 0, Tock = 0 };

        /// <summary>Seconds since start pressed.</summary>
        public static double RealTime
        {
            get { return (DateTime.Now - _startTime).TotalSeconds; }
        }

        /// <summary>Current user settings.</summary>
        public static UserSettings TheSettings { get; set; } = new UserSettings();
    }
}
