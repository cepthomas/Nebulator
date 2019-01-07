using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebulator.Synth
{
    public class SynthCommon
    {
        // TODON2 could make these vars.
        public const int SAMPLE_RATE = 44100;
        public const int NUM_OUTPUTS = 2;

        public const double ONE_OVER_128 = 0.0078125;
        public const double ONE_PI = Math.PI;
        public const double TWO_PI = Math.PI * 2;
        public const double RAD_PER_SAMPLE = TWO_PI / SAMPLE_RATE;
        public const double SQRT2 = 1.41421356237309504880;

        public const int MAX_NOTE = 127;

        /// Compare two doubles "close enough".
        public static bool Close(double t1, double t2)
        {
            return Math.Abs(t2 - t1) < 0.000001;
        }

        /// App run unique id.
        static int _id = 1;
        public static int NextId()
        {
            return _id++;
        }
    }
}
