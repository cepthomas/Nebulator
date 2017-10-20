using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Nebulator.Common;

namespace Nebulator.FastTimer
{
    /// <summary>Diagnostics for timing measurement.</summary>
    public class TimingAnalyzer
    {
        #region Fields
        Stopwatch _watch = new Stopwatch();
        List<double> _times = new List<double>();
        long _lastTicks = -1;
        #endregion

        #region Properties
        /// <summary>Number of data points.</summary>
        public long Count { get { return _times.Count; } }

        /// <summary>Mean in msec.</summary>
        public double Mean { get; private set; } = 0;

        /// <summary>Min in msec.</summary>
        public double Min { get; private set; } = 0;

        /// <summary>Max in msec.</summary>
        public double Max { get; private set; } = 0;

        /// <summary>SD in msec.</summary>
        public double SD { get; private set; } = 0;
        #endregion

        /// <summary>
        /// Stop accumulator.
        /// </summary>
        public void Stop()
        {
            _watch.Stop();
            // Process the collected stuff.
            Mean = _times.Average();
            Max = _times.Max();
            Min = _times.Min();
            SD = Utils.StandardDeviation(_times.ConvertAll(v => v));
        }

        /// <summary>
        /// Clear accumulator.
        /// </summary>
        public void Clear()
        {
            Mean = 0;
            Max = 0;
            Min = 0;
            SD = 0;
            _lastTicks = -1;
            _times.Clear();
        }

        /// <summary>
        /// Grab a data point.
        /// </summary>
        public void Grab()
        {
            if(_lastTicks == -1)
            {
                // Autostart.
                _times.Clear();
                _watch.Start();
                _lastTicks = _watch.ElapsedTicks;
            }
            else
            {
                // Increment.
                long t = _watch.ElapsedTicks; // snap!
                double dt = TicksToMsec(t - _lastTicks);
                _times.Add(dt);
                _lastTicks = t;
            }
        }

        /// <summary>
        /// Conversion.
        /// </summary>
        /// <param name="ticks"></param>
        /// <returns></returns>
        double TicksToMsec(long ticks)
        {
            return 1000.0 * ticks / Stopwatch.Frequency;
        }

        /// <summary>
        /// Readable.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Count:{Count} Mean:{Mean:F2} Max:{Max:F2} Min:{Min:F2} SD:{SD:F2}";
        }
    }
}
