
using System;


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
        protected Filter()
        {
            //...
        }
        #endregion

        #region Public Functions

        #endregion

        #region Private functions
        // This was CK_DDN - dedenormal. Doesn't seem to do that to my eye.
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
            double pfreq = freq * SynthCommon.RAD_PER_SAMPLE * 0.5;
            
            double C = 1.0 / Math.Tan(pfreq);
            double C2 = C * C;
            double sqrt2C = C * SynthCommon.SQRT2;
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
            double pfreq = freq * SynthCommon.RAD_PER_SAMPLE * 0.5;
            double C = Math.Tan(pfreq);
            double C2 = C * C;
            double sqrt2C = C * SynthCommon.SQRT2;
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
            double pfreq = freq * SynthCommon.RAD_PER_SAMPLE;
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
            double pfreq = freq * SynthCommon.RAD_PER_SAMPLE;
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
            double pfreq = freq * SynthCommon.RAD_PER_SAMPLE;
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
            double pfreq = freq * SynthCommon.RAD_PER_SAMPLE;
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
            double pfreq = freq * SynthCommon.RAD_PER_SAMPLE;
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
            _a2 = (_prad * _prad);
            _a1 = (-2.0 * _prad * Math.Cos(2.0 * SynthCommon.ONE_PI * _pfreq / SynthCommon.SAMPLE_RATE));

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
            _b2 = (_zrad * _zrad);
            _b1 = (-2.0 * _zrad * Math.Cos(2.0 * SynthCommon.ONE_PI * _zfreq / SynthCommon.SAMPLE_RATE));
        }
        #endregion
    }
}
