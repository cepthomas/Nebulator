using System;
using System.Collections.Generic;
using System.Linq;


namespace Nebulator.Common
{
    /// <summary>
    /// Statistical randomizer for time and volume
    /// </summary>
    public class Wobbler
    {
        Random _rand = new Random();

        /// <summary>Minimum allowed value.</summary>
        public int Min { get; set; } = 0;

        /// <summary>Maximum allowed value.</summary>
        public int Max { get; set; } = 100;

        /// <summary>Minimum range for randomizing - 3 sigma.</summary>
        public int RangeLow { get; set; } = 0;

        /// <summary>Maximum range for randomizing - 3 sigma.</summary>
        public int RangeHigh { get; set; } = 0;

        /// <summary>
        /// Return next from standard distribution.
        /// </summary>
        /// <param name="val">Center distribution around this.</param>
        /// <returns>Randomized value or val if Min/Max are 0.</returns>
        public int Next(int val)
        {
            int newVal = val; // default

            if(RangeLow != 0 || RangeHigh != 0)
            {
                int max = val + RangeHigh;
                int min = val + RangeLow;
                int mean = min + (max - min) / 2;
                int sigma = (max - min) / 3; // 3 sd
                newVal = (int)Utils.NextGaussian(_rand, mean, sigma);
                newVal = Utils.Constrain(newVal, Min, Max);
            }
            return newVal;
        }

        /// <summary>
        /// Return next from standard distribution.
        /// </summary>
        /// <param name="val">Center distribution around this.</param>
        /// <returns>Randomized value or val if Min/Max are 0.</returns>
        public Time Next(Time val)
        {
            int newVal = Next(val.TotalTocks);
            return new Time(newVal);
        }
    }
}
