
using System;
using System.Collections.Generic;

namespace Nebulator.Synth
{
    ////// from ugen_xxx.* ///////

// TODOX2 stereo again:
// UGen u;
// // standard mono connection
// u => dac;
// // access stereo halves
// u => dac.left;
// u => dac.right;
// adc functionality mirrors dac.

// // this reverses the stereo image of adc
// adc.right => dac.left;
// adc.left => dac.right;
// If you have your great UGen network going and you want to throw it somewhere in the stereo ﬁeld you can use Pan2. You can use the .pan function to move your sound between left (-1) and right (1).

// // this is a stereo connection to the dac
// SinOsc s => Pan2 p => dac;
// 1 => p.pan;
// while(1::second => now){
//   // this will flip the pan from left to right
//   p.pan() * -1. => p.pan;
// }
// You can also mix down your stereo signal to a mono signal using the Mix2 object.

// adc => Mix2 m => dac.left;
// If you remove the Mix2 in the chain and replace it with a Gain object it will act the same way. When you connect a stereo object to a mono object it will sum the inputs. You will get the same effect as if you connect two mono signals to the input of another mono signal.




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