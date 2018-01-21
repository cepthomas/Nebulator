using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nebulator.Common;
using Nebulator.Midi;

namespace Nebulator.Dynamic
{
    /// <summary>
    /// General globals we might want at runtime.
    /// </summary>
    public class DynamicEntities // https://msdn.microsoft.com/en-us/library/hh242977(v=vs.103).aspx
    {
        #region Backing fields
        static bool _playing = false;
        static DateTime _startTime = DateTime.Now;
        #endregion



        /// <summary>Playing the part.</summary>
        public static bool Playing
        {
            get { return _playing; }
            set { _playing = value; if (_playing) _startTime = DateTime.Now; }
        }

        /// <summary>Current step time clock.</summary>
        public static Time StepTime { get; set; } = new Time() { Tick = 0, Tock = 0 };

        /// <summary>Seconds since start pressed.</summary>
        public static double RealTime
        {
            get { return (DateTime.Now - _startTime).TotalSeconds; }
        }




        /// <summary>Master speed in bpm.</summary>
        public static double Speed { get; set; } = 80.0;

        /// <summary>Master volume.</summary>
        public static int Volume { get; set; } = 80;




        #region Functions
        /// <summary>Don't even try to do this.</summary>
        DynamicEntities() { }

        /// <summary>Reset everything.</summary>
        public static void Clear()
        {
            Playing = false;
            StepTime = new Time();
        }

        #endregion
    }
}
