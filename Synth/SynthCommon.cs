using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebulator.Synth
{
    public class SynthCommon
    {
        public const int SAMPLE_RATE = 44100;

        public const double ONE_OVER_128 = 0.0078125;

        /// Convert float note to frequency.
        public static double NoteToFreq(double noteNumber)
        {
            double frequency = 220.0 * Math.Pow(2.0, (noteNumber - 57.0) / 12.0);
            return frequency;
        }

        /// Compare two doubles close enough for this app.
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
