
using System;


/////// From NAudio:
// based on Cookbook formulae for audio EQ biquad filter coefficients
// http://www.musicdsp.org/files/Audio-EQ-Cookbook.txt
// by Robert Bristow-Johnson  <rbj@audioimagination.com>
//
//    alpha = sin(w0)/(2*Q)                                       (case: Q)
//          = sin(w0)*sinh( ln(2)/2 * BW * w0/sin(w0) )           (case: BW)
//          = sin(w0)/2 * sqrt( (A + 1/A)*(1/S - 1) + 2 )         (case: S)
//
// Q: (the EE kind of definition, except for peakingEQ in which A*Q is
// the classic EE Q. That adjustment in definition was made so that
// a boost of N dB followed by a cut of N dB for identical Q and
// f0/Fs results in a precisely flat unity gain filter or "wire".)
//
// BW: the bandwidth in octaves (between -3 dB frequencies for BPF
// and notch or between midpoint (dBgain/2) gain frequencies for
// peaking EQ)
//
// S: a "shelf slope" parameter (for shelving EQ only). When S = 1,
// the shelf slope is as steep as it can be and remain monotonically
// increasing or decreasing gain with frequency. The shelf slope, in
// dB/octave, remains proportional to S for all other values for a
// fixed f0/Fs and dBgain.



namespace Nebulator.Synth
{
    /// <summary>
    /// Filter virtual base class.
    /// </summary>
    public abstract class Filter : UGen
    {
        #region Fields TODON3 clean up?
        protected double _a0;
        protected double _a1;
        protected double _a2;
        protected double _b0;
        protected double _b1;
        protected double _b2;
        protected double _y1;
        protected double _y2;

        protected double _freq;
        protected double _pfreq;
        protected double _zfreq;
        protected double _prad;
        protected double _zrad;
        protected double _q;
        protected double _db;
        protected bool _norm;

        protected double _input0;
        protected double _input1;
        protected double _input2;
        protected double _output0;
        protected double _output1;
        protected double _output2;
        #endregion

        #region Private functions
        /// <summary>
        /// Dedenormalize value. Was CK_DDN macro.
        /// </summary>
        protected double DDN(double f)
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
    /// 2nd order low pass Butterworth filter.
    /// </summary>
    public class LPF : Filter
    {
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
            _y1 = DDN(_y1);
            _y2 = DDN(_y2);

            return dout * Volume;
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
            double nextA0 = 1.0 / (1.0 + sqrt2C + C2);
            double nextB1 = -2.0 * (1.0 - C2) * nextA0 ;
            double nextB2 = -(1.0 - sqrt2C + C2) * nextA0;

            _freq = freq;
            _a0 = nextA0;
            _b1 = nextB1;
            _b2 = nextB2;
        }
        #endregion
    }

    /// <summary>
    /// 2nd order high pass Butterworth filter.
    /// </summary>
    public class HPF : Filter
    {
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
            _y1 = DDN(_y1);
            _y2 = DDN(_y2);

            return dout * Volume;
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
            double nextA0 = 1.0 / (1.0 + sqrt2C + C2);
            double nextB1 = 2.0 * (1.0 - C2) * nextA0 ;
            double nextB2 = -(1.0 - sqrt2C + C2) * nextA0;

            _freq = freq;
            _a0 = nextA0;
            _b1 = nextB1;
            _b2 = nextB2;
        }
        #endregion
    }

    /// <summary>
    /// 2nd order band pass Butterworth filter.
    /// </summary>
    public class BPF : Filter
    {
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
            _y1 = DDN(_y1);
            _y2 = DDN(_y2);

            return dout * Volume;
        }
        #endregion

        #region Private functions
        void SetParams(double freq, double q)
        {
            double pfreq = freq * Math.PI * 2 / SynthCommon.SampleRate;
            double pbw = 1.0 / q * pfreq * .5;

            double C = 1.0 / Math.Tan(pbw);
            double D = 2.0 * Math.Cos(pfreq);
            double nextA0 = 1.0 / (1.0 + C);
            double nextB1 = C * D * nextA0 ;
            double nextB2 = (1.0 - C) * nextA0;

            _freq = freq;
            _q = q;
            _a0 = nextA0;
            _b1 = nextB1;
            _b2 = nextB2;
        }
        #endregion
    }

    /// <summary>
    /// 2nd order band reject pass Butterworth filter.
    /// </summary>
    public class BRF : Filter
    {
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
            _y1 = DDN(_y1);
            _y2 = DDN(_y2);

            return dout * Volume;
        }
        #endregion

        #region Private functions
        void SetParams( double freq, double q )
        {
            double pfreq = freq * Math.PI * 2 / SynthCommon.SampleRate;
            double pbw = 1.0 / q * pfreq * .5;
            double C = Math.Tan(pbw);
            double D = 2.0 * Math.Cos(pfreq);
            double nextA0 = 1.0 / (1.0 + C);
            double nextB1 = -D * nextA0 ;
            double nextB2 = (1.0 - C) * nextA0;

            _freq = freq;
            _q = q;
            _a0 = nextA0;
            _b1 = nextB1;
            _b2 = nextB2;
        }
        #endregion
    }

    /// <summary>
    /// 2nd order low pass Butterworth filter with resonance.
    /// </summary>
    public class RLPF : Filter
    {
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
            _y1 = DDN(_y1);
            _y2 = DDN(_y2);

            return dout * Volume;
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
            double nextB1 = (1.0 + C) * cosf;
            double nextB2 = -C;
            double nextA0 = (1.0 + C - nextB1) * 0.25;

            _freq = freq;
            _q = 1.0 / qres;
            _a0 = nextA0;
            _b1 = nextB1;
            _b2 = nextB2;
        }
        #endregion
    }

    /// <summary>
    /// 2nd order high pass Butterworth filter with resonance.
    /// </summary>
    public class RHPF : Filter
    {
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
            _y1 = DDN(_y1);
            _y2 = DDN(_y2);

            return dout * Volume;
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
            double nextB1 = (1.0 + C) * cosf;
            double nextB2 = -C;
            double nextA0 = (1.0 + C + nextB1) * 0.25;

            _freq = freq;
            _q = 1.0 / qres;
            _a0 = nextA0;
            _b1 = nextB1;
            _b2 = nextB2;
        }
        #endregion
    }

    /// <summary>
    /// Resonance filter. Same as BiQuad with equal gain zeros.
    /// </summary>
    public class ResonZ : Filter
    {
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
            _y1 = DDN(_y1);
            _y2 = DDN(_y2);

            return dout * Volume;
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
            double nextB1 = R2 * cost;
            double nextB2 = -R22;
            double nextA0 = (1.0 - R22) * 0.5;

            _freq = freq;
            _q = q;
            _a0 = nextA0;
            _b1 = nextB1;
            _b2 = nextB2;
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
            _a0 = 1.0;
            _b0 = 1.0;
            _a1 = 0.0;
            _a2 = 0.0;
            _b1 = 0.0;
            _b2 = 0.0;

            _input0 = 0.0;
            _input1 = 0.0;
            _input2 = 0.0;
            _output0 = 0.0;
            _output1 = 0.0;
            _output2 = 0.0;

            _pfreq = 0.0;
            _zfreq = 0.0;
            _prad = 0.0;
            _zrad = 0.0;
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
            _y1 = DDN(_y1);
            _y2 = DDN(_y2);

            return _output0 * Volume;
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



#if TODON2_PORT_THIS_MAYBE

/***************************************************/
/*! \class FormSwep
    \brief STK sweepable formant filter class.

    This public BiQuad filter subclass implements
    a formant (resonance) which can be "swept"
    over time from one frequency setting to another.
    It provides methods for controlling the sweep
    rate and target frequency.

    by Perry R. Cook and Gary P. Scavone, 1995 - 2002.
*/
/***************************************************/


FormSwep :: FormSwep() : BiQuad()
{
    frequency = (double) 0.0;
    radius = (double) 0.0;
    targetGain = (double) 1.0;
    targetFrequency = (double) 0.0;
    targetRadius = (double) 0.0;
    deltaGain = (double) 0.0;
    deltaFrequency = (double) 0.0;
    deltaRadius = (double) 0.0;
    sweepState = (double)  0.0;
    sweepRate = (double) 0.002;
    dirty = false;
    this->clear();
}

FormSwep :: ~FormSwep()
{
}

void FormSwep :: setResonance(double aFrequency, double aRadius)
{
    dirty = false;
    radius = aRadius;
    frequency = aFrequency;

    BiQuad::setResonance( frequency, radius, true );
}

void FormSwep :: setStates(double aFrequency, double aRadius, double aGain)
{
    dirty = false;

    if ( frequency != aFrequency || radius != aRadius )
        BiQuad::setResonance( aFrequency, aRadius, true );

    frequency = aFrequency;
    radius = aRadius;
    gain = aGain;
    targetFrequency = aFrequency;
    targetRadius = aRadius;
    targetGain = aGain;
}

void FormSwep :: setTargets(double aFrequency, double aRadius, double aGain)
{
    dirty = true;
    startFrequency = frequency;
    startRadius = radius;
    startGain = gain;
    targetFrequency = aFrequency;
    targetRadius = aRadius;
    targetGain = aGain;
    deltaFrequency = aFrequency - frequency;
    deltaRadius = aRadius - radius;
    deltaGain = aGain - gain;
    sweepState = (double) 0.0;
}

void FormSwep :: setSweepRate(double aRate)
{
    sweepRate = aRate;
    if ( sweepRate > 1.0 ) sweepRate = 1.0;
    if ( sweepRate < 0.0 ) sweepRate = 0.0;
}

void FormSwep :: setSweepTime(double aTime)
{
    sweepRate = 1.0 / ( aTime * SynthCommon.SAMPLE_RATE );
    if ( sweepRate > 1.0 ) sweepRate = 1.0;
    if ( sweepRate < 0.0 ) sweepRate = 0.0;
}

double FormSwep :: tick(double sample)
{
    if (dirty)
    {
        sweepState += sweepRate;
        if ( sweepState >= 1.0 )
        {
            sweepState = (double) 1.0;
            dirty = false;
            radius = targetRadius;
            frequency = targetFrequency;
            gain = targetGain;
        }
        else
        {
            radius = startRadius + (deltaRadius * sweepState);
            frequency = startFrequency + (deltaFrequency * sweepState);
            gain = startGain + (deltaGain * sweepState);
        }
        BiQuad::setResonance( frequency, radius, true );
    }

    return BiQuad::tick( sample );
}

double *FormSwep :: tick(double *vec, unsigned int vectorSize)
{
    for (unsigned int i=0; i<vectorSize; i++)
        vec[i] = tick(vec[i]);

    return vec;
}
#endif

}


