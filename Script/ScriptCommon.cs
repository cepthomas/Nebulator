using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Midi;
using NBagOfTricks.Slog;
using MidiLib;
using NBagOfTricks;


namespace Nebulator.Script
{
    public class ScriptCommon
    {
        /// <summary>Resolution for script times.</summary>
        public const int ScriptPpq = 8;

        /// <summary>
        /// Construct a BarTime from Beat.Subdiv representation as a double. Note subdiv (right of dp) part is fixed at ScriptPpq.
        /// </summary>
        /// <param name="beat"></param>
        /// <returns>New BarTime.</returns>
        public static BarTime ToBarTime(double beat)
        {
            var (integral, fractional) = MathUtils.SplitDouble(beat);
            var beats = (int)integral;
            var subdivs = (int)Math.Round(fractional * 10.0);

            if (subdivs >= ScriptPpq)
            {
                throw new Exception($"Invalid subdiv value: {beat}");
            }

            // Scale subdivs to native.
            subdivs = subdivs * Definitions.InternalPPQ / ScriptPpq;
            var totalSubdivs = beats * MidiSettings.LibSettings.SubdivsPerBeat + subdivs;
            return new(totalSubdivs);
        }
    }
}
