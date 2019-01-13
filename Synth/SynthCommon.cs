using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebulator.Synth
{
    public class SynthCommon
    {
        #region General properties
        public static int SampleRate { get; set; } = 44100;
        public static int NumOutputs { get; set; } = 2;
        #endregion

        #region General properties
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
        #endregion
    }
}
