
using System;
using System.Collections.Generic;

namespace Nebulator.Synth
{
    public class AnalyzerEventArgs : EventArgs
    {
        /// <summary>Max value.</summary>
        public double MaxLeft { get; set; } = 0;

        /// <summary>Max value.</summary>
        public double MaxRight { get; set; } = 0;
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
        public double MaxLeft { get; set; } = 0;

        /// <summary>
        /// Collected max value.
        /// </summary>
        public double MaxRight { get; set; } = 0;
        #endregion

        #region Fields
        /// <summary>
        /// Approx 10 times per sec.
        /// </summary>
        const int BUFF_SIZE = 5000;

        /// <summary>
        /// Allocate buffer now to minimize work during Read();
        /// </summary>
        Sample[] _buff = new Sample[BUFF_SIZE];

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
        public override Sample Next2(Sample din)
        {
            if(AnalyzerEvent != null)
            {
                _buff[_buffIndex] = din;
                _buffIndex++;

                if (_buffIndex >= BUFF_SIZE)
                {
                    double maxL = 0;
                    double maxR = 0;

                    for (int i = 0; i < BUFF_SIZE; i++)
                    {
                        double val = Math.Abs(_buff[i].Left);
                        maxL = Math.Max(maxL, val);

                        val = Math.Abs(_buff[i].Right);
                        maxR = Math.Max(maxR, val);

                        _buff[i].Left = 0;
                        _buff[i].Right = 0;
                    }

                    MaxLeft = maxL;
                    MaxRight = maxR;
                    _buffIndex = 0;

                    args.MaxLeft = maxL;
                    args.MaxRight = maxR;
                    AnalyzerEvent(this, args);
                }
            }

            // Always pass value through.
            return din;
        }
        #endregion
    }
}