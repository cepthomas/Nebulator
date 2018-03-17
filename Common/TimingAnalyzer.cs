using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Nebulator.Common;

namespace Nebulator.Common
{
    /// <summary>Diagnostics for timing measurement.</summary>
    public class TimingAnalyzer
    {
        #region Fields
        /// <summary>The internal timer.</summary>
        Stopwatch _watch = new Stopwatch();

        /// <summary>Accumulated data points.</summary>
        List<double> _times = new List<double>();

        /// <summary>Last grab time for calculating diff.</summary>
        long _lastTicks = -1;
        #endregion

        #region Properties
        /// <summary>Number of data points to grab for statistics.</summary>
        public long SampleSize { get; set; } = 50;

        /// <summary>Number of data points.</summary>
        public long Count { get; set; } = 0;

        /// <summary>Mean in msec.</summary>
        public double Mean { get; set; } = 0;

        /// <summary>Min in msec.</summary>
        public double Min { get; set; } = 0;

        /// <summary>Max in msec.</summary>
        public double Max { get; set; } = 0;

        /// <summary>SD in msec.</summary>
        public double SD { get; set; } = 0;
        #endregion

        /// <summary>Readable.</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Count:{Count} Mean:{Mean:F3} Max:{Max:F3} Min:{Min:F3} SD:{SD:F3}";
        }


        /// <summary>
        /// Stop accumulator.
        /// </summary>
        public void Stop()
        {
            _watch.Stop();
            _times.Clear();
            _lastTicks = -1;
        }

        /// <summary>
        /// Execute this before measuring the duration of something.
        /// </summary>
        public void Arm()
        {
            if (!_watch.IsRunning)
            {
                _lastTicks = -1;
                _watch.Start();
            }

            _lastTicks = _watch.ElapsedTicks;
        }

        /// <summary>
        /// Do one read since Arm().
        /// </summary>
        /// <returns></returns>
        public double ReadOne()
        {
            double dt = 0.0;
            long t = _watch.ElapsedTicks; // snap!

            if (_lastTicks != -1)
            {
                dt = Utils.TicksToMsec(t - _lastTicks);
                _times.Add(dt);
            }
            _lastTicks = t;
            return dt;
        }

        /// <summary>
        /// Grab a data point. Also auto starts the timer.
        /// </summary>
        /// <returns>New stats are available.</returns>
        public bool Grab()
        {
            bool stats = false;

            if(!_watch.IsRunning)
            {
                _times.Clear();
                _lastTicks = -1;
                _watch.Start();
            }

            long t = _watch.ElapsedTicks; // snap!

            if (_lastTicks != -1)
            {
                double dt = Utils.TicksToMsec(t - _lastTicks);
                _times.Add(dt);
            }
            _lastTicks = t;

            if (_times.Count >= SampleSize)
            {
                // Process the collected stuff.
                Mean = _times.Average();
                Max = _times.Max();
                Min = _times.Min();
                SD = Utils.StandardDeviation(_times.ConvertAll(v => v));
                Count = _times.Count;

                stats = true;

                _times.Clear();
            }

            return stats;
        }
    }
}
