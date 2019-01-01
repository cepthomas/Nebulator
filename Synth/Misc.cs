
using System;
using System.Collections.Generic;

namespace Nebulator.Synth
{
    ////// from ugen_xxx.* ///////

    public class Mix : UGen
    {
        #region Fields
        #endregion

        #region Properties
        #endregion

        #region Lifecycle
        public Mix()
        {
        }
        #endregion

        #region Public Functions
        #endregion

        #region Private functions
        #endregion

        public double Next(params double[] din)
        {
            double dout = 0;
            for (int i = 0; i < din.Length; i++)
            {
                dout += din[i];
            }
            return dout * Gain;
        }
    }

    public class Pan : UGen
    {
        #region Fields
        #endregion

        #region Properties
        // -1 to +1
        public double Location { get; set; } = 0.0;
        #endregion

        #region Lifecycle
        #endregion

        #region Public Functions
        #endregion

        #region Private functions
        #endregion

        public Pan()
        {
        }

        public override Sample Next2(double din)
        {
            Sample dout = new Sample
            {
                Left = din * -Location,
                Right = din * Location
            };
            return dout * Gain;
        }
    }
}