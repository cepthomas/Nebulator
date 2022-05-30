using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//TODOX temp home while moving. Remove later.


namespace Nebulator.Common
{
    /// <summary>Helpers to translate between midi standard and arbtrary internal representation.</summary>
    public class MidiTime
    {
        /// <summary>Resolution for midi events aka DeltaTicksPerQuarterNote.</summary>
        public int MidiPpq { get; set; } = 96;

        /// <summary>Resolution for internal format.</summary>
        public int InternalPpq { get; set; } = 4;

        /// <summary>Tempo aka BPM.</summary>
        public double Tempo { get; set; } = 0.0;

        ///// <summary>Time signature Future?</summary>
        // public string TimeSig { get; set; } = "";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public long InternalToMidi(int t)
        {
            long mtime = t * MidiPpq / InternalPpq;
            return mtime;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public int MidiToInternal(long t)
        {
            long itime = t * InternalPpq / MidiPpq;
            return (int)itime;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public double InternalToMsec(int t)
        {
            double msec = InternalPeriod() * t;
            return msec;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public double MidiToSec(int t)
        {
            double msec = MidiPeriod() * t / 1000.0;
            return msec;
        }

        /// <summary>
        /// Exact time between events.
        /// </summary>
        /// <returns></returns>
        public double MidiPeriod()
        {
            double secPerBeat = 60.0 / Tempo;
            double msecPerT = 1000 * secPerBeat / MidiPpq;
            return msecPerT;
        }

        /// <summary>
        /// Exact time between events.
        /// </summary>
        /// <returns></returns>
        public double InternalPeriod()
        {
            double secPerBeat = 60.0 / Tempo;
            double msecPerT = 1000 * secPerBeat / InternalPpq;
            return msecPerT;
        }

        /// <summary>
        /// Integer time between events.
        /// </summary>
        /// <returns></returns>
        public int RoundedInternalPeriod()
        {
            double msecPerT = InternalPeriod();
            int period = msecPerT > 1.0 ? (int)Math.Round(msecPerT) : 1;
            return period;
        }
    }
}