
using System;
using System.Collections.Generic;

namespace Nebulator.Synth
{
    public class AnalyzerEventArgs : EventArgs
    {
        /// <summary>Received data.</summary>
        public double MaxValue { get; set; } = 0;
    }

    public class Analyzer : UGen
    {
        /// <summary>
        /// Raised periodically to inform the user of things like max volume.
        /// </summary>
        public event EventHandler<AnalyzerEventArgs> AnalyzerEvent;

        /// <summary>
        /// Avoid GC issues in Next(). TODON2 and others?
        /// </summary>
        AnalyzerEventArgs args;

        #region Properties
        /// <summary>
        /// Collected max value.
        /// </summary>
        public double MaxValue { get; set; } = 0;
        #endregion

        #region Fields
        /// <summary>
        /// Approx 10 times per sec.
        /// </summary>
        const int BUFF_SIZE = 5000;

        /// <summary>
        /// Allocate buffer now to minimize work during Read();
        /// </summary>
        double[] _buff = new double[BUFF_SIZE];

        /// <summary>
        /// Where we are in the buffer.
        /// </summary>
        int _buffIndex = 0;
        #endregion

        #region Public Functions
        /// <summary>
        /// Constructor.
        /// </summary>
        public Analyzer()
        {
            args = new AnalyzerEventArgs() { };
        }

        /// <inheritdoc />
        public override double Next(double din)
        {
            if(AnalyzerEvent != null)
            {
                _buff[_buffIndex] = din;
                _buffIndex++;

                if (_buffIndex >= BUFF_SIZE)
                {
                    double max = 0;

                    for (int i = 0; i < BUFF_SIZE; i++)
                    {
                        double val = Math.Abs(_buff[i]);
                        _buff[i] = 0;
                        max = Math.Max(max, val);
                    }

                    MaxValue = max;
                    _buffIndex = 0;

                    args.MaxValue = max;
                    AnalyzerEvent(this, args);
                }
            }

            // Always pass value through.
            return din;
        }
        #endregion
    }
}