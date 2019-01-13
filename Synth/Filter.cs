
using System;


//From NAudio:
// based on Cookbook formulae for audio EQ biquad filter coefficients
// http://www.musicdsp.org/files/Audio-EQ-Cookbook.txt
// by Robert Bristow-Johnson  <rbj@audioimagination.com>

//    alpha = sin(w0)/(2*Q)                                       (case: Q)
//          = sin(w0)*sinh( ln(2)/2 * BW * w0/sin(w0) )           (case: BW)
//          = sin(w0)/2 * sqrt( (A + 1/A)*(1/S - 1) + 2 )         (case: S)
// Q: (the EE kind of definition, except for peakingEQ in which A*Q is
// the classic EE Q.  That adjustment in definition was made so that
// a boost of N dB followed by a cut of N dB for identical Q and
// f0/Fs results in a precisely flat unity gain filter or "wire".)
//
// BW: the bandwidth in octaves (between -3 dB frequencies for BPF
// and notch or between midpoint (dBgain/2) gain frequencies for
// peaking EQ)
//
// S: a "shelf slope" parameter (for shelving EQ only).  When S = 1,
// the shelf slope is as steep as it can be and remain monotonically
// increasing or decreasing gain with frequency.  The shelf slope, in
// dB/octave, remains proportional to S for all other values for a
// fixed f0/Fs and dBgain.



namespace Nebulator.Synth
{
    /// <summary>
    /// Filter virtual base class.
    /// </summary>
    public abstract class Filter : UGen
    {
        #region Fields
        protected double _a0;
        protected double _a1;
        protected double _a2;
        protected double _b0;
        protected double _b1;
        protected double _b2;
        protected double _y1;
        protected double _y2;
        protected double _input0;
        protected double _input1;
        protected double _input2;
        protected double _output0;
        protected double _output1;
        protected double _output2;
        protected double _freq;
        protected double _pfreq;
        protected double _zfreq;
        protected double _prad;
        protected double _zrad;
        protected double _q;
        protected double _db;
        protected bool _norm;
        #endregion

        #region Properties
        #endregion

        #region Lifecycle
        #endregion

        #region Public Functions
        #endregion

        #region Private functions
        // This was CK_DDN - dedenormal. Doesn't seem to do that to my eye.
        // Found!
        /* denormals are very small floating point numbers that force FPUs into slow
        mode. All lowpass filters using floats are suspectible to denormals unless
        a small offset is added to avoid very small floating point numbers. */
        //#define DENORMAL_OFFSET (1E-10)
        ///
        // When the value to be represented is too small to encode normally, it is encoded in denormalized form, indicated by an exponent value of Float.MIN_EXPONENT - 1 or Double.MIN_EXPONENT - 1. Denormalized floating-point numbers have an assumed 0 in the ones' place and have one or more leading zeros in the represented portion of their mantissa. These leading zero bits no longer function as significant bits of precision; consequently, the total precision of denormalized floating-point numbers is less than that of normalized floating-point numbers. Note that even using normalized numbers where precision is required can pose a risk. See rule NUM04-J. Do not use floating-point numbers if precise computation is required for more information.

        // Using denormalized numbers can severely impair the precision of floating-point calculations; as a result, denormalized numbers must not be used.

        // Detecting Denormalized Numbers
        // The following code tests whether a float value is denormalized in FP-strict mode or for platforms that lack extended range support. Testing for denormalized numbers in the presence of extended range support is platform-dependent; see rule NUM53-J. Use the strictfp modifier for floating-point calculation consistency across platforms for additional information.

        // strictfp public static boolean isDenormalized(float val) {
        //   if (val == 0) {
        //     return false;
        //   }
        //   if ((val > -Float.MIN_NORMAL) && (val < Float.MIN_NORMAL)) {
        //     return true;
        //   }
        //   return false;
        // }
        // Testing whether values of type double are denormalized is analogous.


        ///// or:
        // You can repair a denormal by checking flags in a floating point number and substituting zero. But that’s a lot of work. A simple way to avoid denormals is to add a tiny but normalized number to calculations that are at risk—1E-18, for instance is -200 dB down from unity, and won’t affect audio paths, but when added to denormals will result in 1E-18; this won’t affect audible samples at all, due to the way floating point works.
        // While denormal protection could be built into the filter code, it’s more economical to use denormal protection on a more global level, instead of in each module. As an example of one possible solution, you could add a tiny constant (1E-18) to every incoming sample at the beginning of your processing chain, switching the sign of the constant (to -1E-18) after every buffer you process to avoid the possibility of a highpass filter in the chain removing the constant offset.
        // Another variant in the x86/amd64 case is to set the FPU flush denormals to zero bit for the thread handling audio data. I’ve had an implementation with lots of parallell biquads that prior to that suffered from denormals, but with the bit set, works like a charm. The advantage is of course that you do not need to fiddle with the de-denormal constant.



        protected double DDN(double f) // was a macro
        {
            double n;
            if(f >= 0)
            {
                n = f > 1e-15 && f < 1e15 ? f : 0.0;
            }
            else
            {
                n = f < -1e-15 && f > -1e15 ? f : 0.0;
            }
            return n;
        }
        #endregion
    }

    /// <summary>
    /// Filter class.
    /// </summary>
    public class LPF : Filter
    {
        #region Fields
        #endregion

        #region Properties
        public double Freq
        {
            get { return _freq; }
            set { SetParams(value); }
        }
        #endregion

        #region Lifecycle
        public LPF()
        {
            _freq = 0.0;
            _q = 1.0;
        }
        #endregion

        #region Public Functions
        public override double Next(double din)
        {
            double y0;
            double dout;
            
            // go: adapted from SC3's LPF
            y0 = din + _b1 * _y1 + _b2 * _y2;
            dout = _a0 * (y0 + 2 * _y1 + _y2);
            _y2 = _y1;
            _y1 = y0;

            // be normal
            DDN(_y1);
            DDN(_y2);

            return dout * Gain;
        }
        #endregion

        #region Private functions
        void SetParams(double freq)
        {
            // implementation: adapted from SC3's LPF
            double pfreq = freq * Math.PI * 2 / SynthCommon.SampleRate * 0.5;
            
            double C = 1.0 / Math.Tan(pfreq);
            double C2 = C * C;
            double sqrt2C = C * Math.Sqrt(2);
            double next_a0 = 1.0 / (1.0 + sqrt2C + C2);
            double next_b1 = -2.0 * (1.0 - C2) * next_a0 ;
            double next_b2 = -(1.0 - sqrt2C + C2) * next_a0;

            _freq = freq;
            _a0 = next_a0;
            _b1 = next_b1;
            _b2 = next_b2;
        }
        #endregion
    }

    /// <summary>
    /// Filter class.
    /// </summary>
    public class HPF : Filter
    {
        #region Fields
        #endregion

        #region Properties
        public double Freq
        {
            get { return _freq; }
            set { SetParams(value); }
        }
        #endregion

        #region Lifecycle
        public HPF()
        {
            _freq = 0.0;
            _q = 1.0;
        }
        #endregion

        #region Public Functions
        public override double Next(double din)
        {
            // go: adapted from SC3's HPF
            double y0 = din + _b1 * _y1 + _b2 * _y2;
            double dout = _a0 * (y0 - 2 * _y1 + _y2);
            _y2 = _y1;
            _y1 = y0;

            // be normal
            DDN(_y1);
            DDN(_y2);

            return dout * Gain;
        }
        #endregion

        #region Private functions
        void SetParams(double freq)
        {
            // implementation: adapted from SC3's HPF
            double pfreq = freq * Math.PI * 2 / SynthCommon.SampleRate * 0.5;
            double C = Math.Tan(pfreq);
            double C2 = C * C;
            double sqrt2C = C * Math.Sqrt(2);
            double next_a0 = 1.0 / (1.0 + sqrt2C + C2);
            double next_b1 = 2.0 * (1.0 - C2) * next_a0 ;
            double next_b2 = -(1.0 - sqrt2C + C2) * next_a0;

            _freq = freq;
            _a0 = next_a0;
            _b1 = next_b1;
            _b2 = next_b2;
        }
        #endregion
    }

    /// <summary>
    /// Filter class.
    /// </summary>
    public class BPF : Filter
    {
        #region Fields
        #endregion

        #region Properties
        public double Freq
        {
            get { return _freq; }
            set { SetParams(value, _q); }
        }

        public double Q
        {
            get { return _q; }
            set { SetParams(_freq, value); }
        }
        #endregion

        #region Lifecycle
        public BPF()
        {
            _freq = 0.0;
            _q = 1.0;
        }
        #endregion

        #region Public Functions
        public override double Next(double din)
        {
            // go: adapted from SC3's LPF
            double y0 = din + _b1 * _y1 + _b2 * _y2;
            double dout = _a0 * (y0 - _y2);
            _y2 = _y1;
            _y1 = y0;

            // be normal
            DDN(_y1);
            DDN(_y2);

            return dout * Gain;
        }
        #endregion

        #region Private functions
        void SetParams(double freq, double q)
        {
            double pfreq = freq * Math.PI * 2 / SynthCommon.SampleRate;
            double pbw = 1.0 / q * pfreq * .5;

            double C = 1.0 / Math.Tan(pbw);
            double D = 2.0 * Math.Cos(pfreq);
            double next_a0 = 1.0 / (1.0 + C);
            double next_b1 = C * D * next_a0 ;
            double next_b2 = (1.0 - C) * next_a0;

            _freq = freq;
            _q = q;
            _a0 = next_a0;
            _b1 = next_b1;
            _b2 = next_b2;
        }
        #endregion
    }

    /// <summary>
    /// Filter class.
    /// </summary>
    public class BRF : Filter
    {
        #region Fields
        #endregion

        #region Properties
        public double Freq
        {
            get { return _freq; }
            set { SetParams( value, _q ); }
        }

        public double Q
        {
            get { return _q; }
            set { SetParams( _freq, value ); }
        }
        #endregion

        #region Lifecycle
        public BRF()
        {
            _freq = 0.0;
            _q = 1.0;
        }
        #endregion

        #region Public Functions
        public override double Next(double din)
        {
            // go: adapted from SC3's HPF
            // b1 is actually a1
            double y0 = din - _b1 * _y1 - _b2 * _y2;
            double dout = _a0 * (y0 + _y2) + _b1 * _y1;
            _y2 = _y1;
            _y1 = y0;

            // be normal
            DDN(_y1);
            DDN(_y2);

            return dout * Gain;
        }
        #endregion

        #region Private functions
        void SetParams( double freq, double q )
        {
            double pfreq = freq * Math.PI * 2 / SynthCommon.SampleRate;
            double pbw = 1.0 / q * pfreq * .5;
            double C = Math.Tan(pbw);
            double D = 2.0 * Math.Cos(pfreq);
            double next_a0 = 1.0 / (1.0 + C);
            double next_b1 = -D * next_a0 ;
            double next_b2 = (1.0 - C) * next_a0;

            _freq = freq;
            _q = q;
            _a0 = next_a0;
            _b1 = next_b1;
            _b2 = next_b2;
        }
        #endregion
    }

    /// <summary>
    /// Filter class.
    /// </summary>
    public class RLPF : Filter
    {
        #region Fields
        #endregion

        #region Properties
        public double Freq
        {
            get { return _freq; }
            set { SetParams( value, _q ); }
        }

        public double Q
        {
            get { return _q; }
            set { SetParams( _freq, value ); }
        }
        #endregion

        #region Lifecycle
        public RLPF()
        {
            _freq = 0.0;
            _q = 1.0;
        }
        #endregion

        #region Public Functions
        public override double Next(double din)
        {
            // go: adapated from SC3's RLPF
            double y0 = _a0 * din + _b1 * _y1 + _b2 * _y2;
            double dout = y0 + 2 * _y1 + _y2;
            _y2 = _y1;
            _y1 = y0;

            // be normal
            DDN(_y1);
            DDN(_y2);

            return dout * Gain;
        }
        #endregion

        #region Private functions
        void SetParams( double freq, double q )
        {
            double qres = Math.Max( .001, 1.0 / q );
            double pfreq = freq * Math.PI * 2 / SynthCommon.SampleRate;
            double D = Math.Tan(pfreq * qres * 0.5);
            double C = (1.0 - D) / (1.0 + D);
            double cosf = Math.Cos(pfreq);
            double next_b1 = (1.0 + C) * cosf;
            double next_b2 = -C;
            double next_a0 = (1.0 + C - next_b1) * 0.25;

            _freq = freq;
            _q = 1.0 / qres;
            _a0 = next_a0;
            _b1 = next_b1;
            _b2 = next_b2;
        }
        #endregion
    }

    /// <summary>
    /// Filter class.
    /// </summary>
    public class RHPF : Filter
    {
        #region Fields
        #endregion

        #region Properties
        public double Freq
        {
            get { return _freq; }
            set { SetParams( value, _q ); }
        }

        public double Q
        {
            get { return _q; }
            set { SetParams( _freq, value ); }
        }
        #endregion

        #region Lifecycle
        public RHPF()
        {
            _freq = 0.0;
            _q = 1.0;
        }
        #endregion

        #region Public Functions
        public override double Next(double din)
        {
            // go: adapted from SC3's RHPF
            double y0 = _a0 * din + _b1 * _y1 + _b2 * _y2;
            double dout = y0 - 2 * _y1 + _y2;
            _y2 = _y1;
            _y1 = y0;

            // be normal
            DDN(_y1);
            DDN(_y2);

            return dout * Gain;
        }
        #endregion

        #region Private functions
        void SetParams( double freq, double q )
        {
            double qres = Math.Max( .001, 1.0 / q );
            double pfreq = freq * Math.PI * 2 / SynthCommon.SampleRate;
            double D = Math.Tan(pfreq * qres * 0.5);
            double C = (1.0 - D) / (1.0 + D);
            double cosf = Math.Cos(pfreq);
            double next_b1 = (1.0 + C) * cosf;
            double next_b2 = -C;
            double next_a0 = (1.0 + C + next_b1) * 0.25;

            _freq = freq;
            _q = 1.0 / qres;
            _a0 = next_a0;
            _b1 = next_b1;
            _b2 = next_b2;
        }
        #endregion
    }

    /// <summary>
    /// Filter class.
    /// </summary>
    public class ResonZ : Filter
    {
        #region Fields
        #endregion

        #region Properties
        public double Freq
        {
            get { return _freq; }
            set { SetParams( value, _q ); }
        }

        public double Q
        {
            get { return _q; }
            set { SetParams( _freq, value ); }
        }
        #endregion

        #region Lifecycle
        public ResonZ()
        {
            SetParams(220, 1);
        }
        #endregion

        #region Public Functions
        public override double Next(double din)
        {
            // go: adapted from SC3's ResonZ
            double y0 = din + _b1 * _y1 + _b2 * _y2;
            double dout = _a0 * (y0 - _y2);
            _y2 = _y1;
            _y1 = y0;

            // be normal
            DDN(_y1);
            DDN(_y2);

            return dout * Gain;
        }
        #endregion

        #region Private functions
        void SetParams( double freq, double q )
        {
            double pfreq = freq * Math.PI * 2 / SynthCommon.SampleRate;
            double B = pfreq / q;
            double R = 1.0 - B * 0.5;
            double R2 = 2.0 * R;
            double R22 = R * R;
            double cost = (R2 * Math.Cos(pfreq)) / (1.0 + R22);
            double next_b1 = R2 * cost;
            double next_b2 = -R22;
            double next_a0 = (1.0 - R22) * 0.5;

            _freq = freq;
            _q = q;
            _a0 = next_a0;
            _b1 = next_b1;
            _b2 = next_b2;
        }
        #endregion
    }

    /// <summary>
    /// BiQuad (two-pole, two-zero) filter class.
    /// </summary>
    public class BiQuad : Filter
    {
        // .b2 (float, READ/WRITE) filter coefficient
        // .b1 (float, READ/WRITE) filter coefficient
        // .b0 (float, READ/WRITE) filter coefficient
        // .a2 (float, READ/WRITE) filter coefficient
        // .a1 (float, READ/WRITE) filter coefficient
        // .a0 (float, READ only) filter coefficient
        // .pfreq (float, READ/WRITE) set resonance frequency (poles)
        // .prad (float, READ/WRITE) pole radius (<= 1 to be stable)
        // .zfreq (float, READ/WRITE) notch frequency
        // .zrad (float, READ/WRITE) zero radius
        // .norm (float, READ/WRITE) normalization
        // .eqzs (float, READ/WRITE) equal gain zeroes// 

        #region Fields
        #endregion

        #region Properties
        public double A0
        {
            get { return _a0; }
            set { _a0 = value; }
        }

        public double A1
        {
            get { return _a1; }
            set { _a1 = value; }
        }
        
        public double A2
        {
            get { return _a2; }
            set { _a2 = value; }
        }

        public double B0
        {
            get { return _b0; }
            set { _b0 = value; }
        }

        public double B1
        {
            get { return _b1; }
            set { _b1 = value; }
        }

        public double B2
        {
            get { return _b2; }
            set { _b2 = value; }
        }

        public double PFreq
        {
            get { return _pfreq; }
            set { _pfreq = value; SetResonance(); }
        }

        public double PRad
        {
            get { return _prad; }
            set { _prad = value; SetResonance(); }
        }

        public double ZFreq
        {
            get { return _zfreq; }
            set { _zfreq = value; SetNotch(); }
        }

        public double ZRad
        {
            get { return _zrad; }
            set { _zrad = value; SetNotch(); }
        }

        public bool Norm
        {
            get { return _norm; }
            set { _norm = value; SetResonance(); }
        }
        #endregion

        #region Lifecycle
        public BiQuad()
        {
            _a0 = _b0 = 1.0;
            _a1 = _a2 = 0.0;
            _b1 = 0.0;
            _b2 = 0.0;

            _input0 = _input1 = _input2 = 0.0;
            _output0 = _output1 = _output2 = 0.0;

            _pfreq = _zfreq = 0.0;
            _prad = _zrad = 0.0;
            _norm = false;
        }
        #endregion

        #region Public Functions
        public override double Next(double din)
        {
            _input0 = _a0 * din;
            _output0 = _b0 * _input0 + _b1 * _input1 + _b2 * _input2;
            _output0 -= _a2 * _output2 + _a1 * _output1;
            _input2 = _input1;
            _input1 = _input0;
            _output2 = _output1;
            _output1 = _output0;

            // be normal
            DDN(_output1);
            DDN(_output2);

            return _output0 * Gain;
        }
        #endregion

        #region Private functions
        void SetResonance()
        {
            _a2 = _prad * _prad;
            _a1 = -2.0 * _prad * Math.Cos(2.0 * Math.PI * _pfreq / SynthCommon.SampleRate);

            if(_norm)
            {
                // Use zeros at +- 1 and normalize the filter peak gain.
                _b0 = 0.5 - 0.5 * _a2;
                _b1 = -1.0;
                _b2 = -_b0;
            }    
        }

        void SetNotch()
        {
            _b2 = _zrad * _zrad;
            _b1 = -2.0 * _zrad * Math.Cos(2.0 * Math.PI * _zfreq / SynthCommon.SampleRate);
        }
        #endregion
    }
}
