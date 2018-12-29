
using System;


namespace Nebulator.Synth
{
    public abstract class Filter : UGen // virtual base
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
        #endregion







//        public override double Sample(double din);
        // public abstract int Sample(float[] buffer, int count);

        // dedenormal?? was CK_DDN hard limit? spec: float 32 bits -3.4E+38 to +3.4E+38
        protected double CK_DDN(double f)
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

        protected Filter()
        {
            //...
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////
    public class LPF : Filter
    {
        #region Fields
        #endregion

        #region Properties
        #endregion

        #region Lifecycle
        public LPF()
        {
            //...
        }
        #endregion

        #region Public Functions
        #endregion

        #region Private functions
        #endregion


        public override double Sample(double din)
        {
            double y0;
            double dout;
            
            // go: adapted from SC3's LPF
            y0 = din + _b1 * _y1 + _b2 * _y2;
            dout = _a0 * (y0 + 2 * _y1 + _y2);
            _y2 = _y1;
            _y1 = y0;

            // be normal
            CK_DDN(_y1);
            CK_DDN(_y2);

            return dout;
        }

        public double Freq
        {
            get { return _freq; }
            set { SetParams(value); }
        }

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
    }

    ////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////
    public class HPF : Filter
    {
        #region Fields
        #endregion

        #region Properties
        #endregion

        #region Lifecycle
        public HPF()
        {
        }
        #endregion

        #region Public Functions
        #endregion

        #region Private functions
        #endregion


        public override double Sample(double din)
        {
            double y0, result;
            
            // go: adapted from SC3's HPF
            y0 = din + _b1 * _y1 + _b2 * _y2;
            result = _a0 * (y0 - 2 * _y1 + _y2);
            _y2 = _y1;
            _y1 = y0;

            // be normal
            CK_DDN(_y1);
            CK_DDN(_y2);

            return result;
        }

        public double Freq
        {
            get { return _freq; }
            set { SetParams(value); }
        }

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
    }

    ////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////
    public class BPF : Filter
    {
        #region Fields
        #endregion

        #region Properties
        #endregion

        #region Lifecycle
        public BPF()
        {
        }
        #endregion

        #region Public Functions
        #endregion

        #region Private functions
        #endregion


        public override double Sample(double din)
        {
            double y0;
            double result;
            
            // go: adapted from SC3's LPF
            y0 = din + _b1 * _y1 + _b2 * _y2;
            result = _a0 * (y0 - _y2);
            _y2 = _y1;
            _y1 = y0;

            // be normal
            CK_DDN(_y1);
            CK_DDN(_y2);

            return result;
        }

        public void SetParams(double freq, double q)
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
    }

    ////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////
    public class BRF : Filter
    {
        #region Fields
        #endregion

        #region Properties
        #endregion

        #region Lifecycle
        #endregion

        #region Public Functions
        #endregion

        #region Private functions
        #endregion

        public BRF()
        {
            //...
        }

        public override double Sample(double din)
        {
            double y0;
            double result;
            
            // go: adapted from SC3's HPF
            // b1 is actually a1
            y0 = din - _b1 * _y1 - _b2 * _y2;
            result = _a0 * (y0 + _y2) + _b1 * _y1;
            _y2 = _y1;
            _y1 = y0;

            // be normal
            CK_DDN(_y1);
            CK_DDN(_y2);

            return result;
        }

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
    }

    ////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////
    public class RLPF : Filter
    {
        #region Fields
        #endregion

        #region Properties
        #endregion

        #region Lifecycle
        #endregion

        #region Public Functions
        #endregion

        #region Private functions
        #endregion

        public RLPF()
        {
            _q = 1.0;
        }

        public override double Sample(double din)
        {
            double y0;
            double result;

            // go: adapated from SC3's RLPF
            y0 = _a0 * din + _b1 * _y1 + _b2 * _y2;
            result = y0 + 2 * _y1 + _y2;
            _y2 = _y1;
            _y1 = y0;

            // be normal
            CK_DDN(_y1);
            CK_DDN(_y2);

            return result;
        }

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
    }

    ////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////
    public class RHPF : Filter
    {
        #region Fields
        #endregion

        #region Properties
        #endregion

        #region Lifecycle
        #endregion

        #region Public Functions
        #endregion

        #region Private functions
        #endregion

        public RHPF()
        {
            _q = 1.0;
        }

        public override double Sample(double din)
        {
            double y0;
            double result;

            // go: adapted from SC3's RHPF
            y0 = _a0 * din + _b1 * _y1 + _b2 * _y2;
            result = y0 - 2 * _y1 + _y2;
            _y2 = _y1;
            _y1 = y0;

            // be normal
            CK_DDN(_y1);
            CK_DDN(_y2);

            return result;
        }

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
    }

    ////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////
    public class ResonZ : Filter
    {
        #region Fields
        #endregion

        #region Properties
        #endregion

        #region Lifecycle
        #endregion

        #region Public Functions
        #endregion

        #region Private functions
        #endregion

        public ResonZ()
        {
            SetParams(220, 1);
        }

        public override double Sample(double din)
        {
            double y0;
            double result;

            // go: adapted from SC3's ResonZ
            y0 = din + _b1 * _y1 + _b2 * _y2;
            result = _a0 * (y0 - _y2);
            _y2 = _y1;
            _y1 = y0;

            // be normal
            CK_DDN(_y1);
            CK_DDN(_y2);

            return result;
        }

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
    }

    ////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////
    public class biquad : Filter
    {
        #region Fields
        #endregion

        #region Properties
        #endregion

        #region Lifecycle
        #endregion

        #region Public Functions
        #endregion

        #region Private functions
        #endregion

        public biquad()
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

        public override double Sample(double din)
        {
            _input0 = _a0 * din;
            _output0 = _b0 * _input0 + _b1 * _input1 + _b2 * _input2;
            _output0 -= _a2 * _output2 + _a1 * _output1;
            _input2 = _input1;
            _input1 = _input0;
            _output2 = _output1;
            _output1 = _output0;

            // be normal
            CK_DDN(_output1);
            CK_DDN(_output2);

            return _output0;
        }

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


        ////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////

        // BiQuad - BiQuad (two-pole, two-zero) filter class.
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


        // TODOX all these
        // public void biquad_ctrl_pfreq(double pfreq)
        // {
        //     SetResonance( d ); 
        // }
        // public void biquad_ctrl_prad(double prad)
        // {
        //     SetResonance( d );
        // }
        // public void biquad_ctrl_zfreq(double zfreq)
        // {
        //     SetNotch( d );
        // }
        // public void biquad_ctrl_zrad(double zrad)
        // {
        //     biquad_data * d = (biquad_data *)OBJ_MEMBER_UINT(SELF, biquad_offset_data );
        //     zrad = GET_CK_FLOAT(ARGS);
        //     SetNotch( d );
        // }
        // public void biquad_ctrl_norm()
        // {
        //     norm = *(bool *)ARGS;
        //     SetResonance( d );
        // }
        // public void biquad_ctrl_pregain(double a0)
        // {
        //     m_a0 = GET_CK_FLOAT(ARGS);
        // }
        // public void biquad_ctrl_eqzs )
        // {
        //     if( *(uint *)ARGS )
        //     {
        //         m_b0 = 1.0f;
        //         m_b1 = 0.0f;
        //         m_b2 = -1.0f;
        //     }
        //     RETURN->v_int = *(uint *)ARGS;
        // }
        // public void biquad_ctrl_b0(double b0)
        // {
        //     m_b0 = b0;
        // }
        // public void biquad_ctrl_b1()
        // {
        //     m_b1 = GET_CK_FLOAT(ARGS);
        // }
        // public void biquad_ctrl_b2 ()
        // {
        //     m_b2 = GET_CK_FLOAT(ARGS);
        // }
        // public void biquad_ctrl_a0 ()
        // {
        //     m_a0 = GET_CK_FLOAT(ARGS);
        // }
        // public void biquad_ctrl_a1 ()
        // {
        //     m_a1 = GET_CK_FLOAT(ARGS);
        // }
        // public void biquad_ctrl_a2 ()
        // {
        //     m_a2 = GET_CK_FLOAT(ARGS);
        // }


    }

    // // TODOX chuck FILTERS - USING STK instead
    // ////////////////////////////////////////////////////////////////////////////////////////////
    // ////////////////////////////////////////////////////////////////////////////////////////////
    // ////////////////////////////////////////////////////////////////////////////////////////////
    // public class onepole : Filter
    // {
    //     public onepole()
    //     {
    //         //...
    //     }

    //     public override int Read(float[] buffer, int offset, int count)
    //     {
    //         throw new System.NotImplementedException();
    //     }

    //     public override void ControlChange(int controlId, object value)
    //     {
    //         throw new System.NotImplementedException();
    //     }

    //     public double Sample(double din)
    //     {
    //         m_input0 = din;
    //         m_output0 = m_b0 * m_input0 - m_a1 * m_output1;
    //         m_output1 = m_output0;

    //         return m_output0;
    //     }

    //     public double Pole
    //     {
    //         //get { return m_a0; }
    //         set { onepole_ctrl_pole(value); }
    //     }

    //     void onepole_ctrl_pole(double p)
    //     {
    //         if( p > 0.0 )
    //             m_b0 = 1.0 - p;
    //         else
    //             m_b0 = 1.0 + p;

    //         m_a0 = -p;
    //     }
    // }


    // ////////////////////////////////////////////////////////////////////////////////////////////
    // ////////////////////////////////////////////////////////////////////////////////////////////
    // ////////////////////////////////////////////////////////////////////////////////////////////
    // public class onezero : Filter
    // {
    //     public onezero()
    //     {
    //         //...
    //     }

    //     public override int Read(float[] buffer, int offset, int count)
    //     {
    //         throw new System.NotImplementedException();
    //     }

    //     public override void ControlChange(int controlId, object value)
    //     {
    //         throw new System.NotImplementedException();
    //     }

    //     public double Sample(double din)
    //     {
    //         m_input0 = din;
    //         m_output0 = m_b1 * m_input1 + m_b0 * m_input0;
    //         m_input1 = m_input0;

    //         return m_output0;
    //     }

    //     public double Zero
    //     {
    //         //get { return xxx; }
    //         set { onezero_ctrl_zero(value); }
    //     }

    //     public void onezero_ctrl_zero(double z)
    //     {
    //         if( z > 0.0 )
    //             m_b0 = 1.0 / ( 1.0 + z );
    //         else
    //             m_b0 = 1.0 / ( 1.0 - z );
                
    //         m_b1 = -z * m_b0;
    //     }
    // }

    // ////////////////////////////////////////////////////////////////////////////////////////////
    // ////////////////////////////////////////////////////////////////////////////////////////////
    // ////////////////////////////////////////////////////////////////////////////////////////////
    // public class twopole : Filter
    // {
    //     public twopole()
    //     {
    //         //...
    //     }

    //     public override int Read(float[] buffer, int offset, int count)
    //     {
    //         throw new System.NotImplementedException();
    //     }

    //     public override void ControlChange(int controlId, object value)
    //     {
    //         throw new System.NotImplementedException();
    //     }

    //     public double Sample(double din)
    //     {
    //         m_input0 = din;
    //         m_output0 = m_b0 * m_input0 - m_a2 * m_output2 - m_a1 * m_output1;
    //         m_output2 = m_output1;
    //         m_output1 = m_output0;

    //         return m_output0;
    //     }

    //     public double Freq
    //     {
    //         get { return pfreq; }
    //         set { twopole_ctrl_freq(value); }
    //     }

    //     public double Rad
    //     {
    //         get { return prad; }
    //         set { twopole_ctrl_rad(value); }
    //     }

    //     public bool Norm
    //     {
    //         get { return norm; }
    //         set { twopole_ctrl_norm(value); }
    //     }

    //     void twopole_ctrl_freq(double freq)
    //     {
    //         pfreq = freq;
    //         SetResonance();
            
    //         if( norm )
    //         {
    //             // Normalize the filter gain ... not terribly efficient. TODOX make function
    //             double real = 1.0 - prad + (m_a2 - prad) * cos( 2.0 * SynthCommon.ONE_PI * pfreq / srate );
    //             double imag = (m_a2 - prad) * sin( 2.0 * SynthCommon.ONE_PI * pfreq / srate );
    //             m_b0 = sqrt( real*real + imag*imag );
    //         }
    //     }

    //     void twopole_ctrl_rad(double rad)
    //     {
    //         prad = rad;
    //         SetResonance();
            
    //         if( norm )
    //         {
    //             // Normalize the filter gain ... not terrbly efficient
    //             double real = 1.0 - prad + (m_a2 - prad) * cos( 2.0 * SynthCommon.ONE_PI * pfreq / srate );
    //             double imag = (m_a2 - prad) * sin( 2.0 * SynthCommon.ONE_PI * pfreq / srate );
    //             m_b0 = sqrt( real*real + imag*imag );
    //         }
    //     }

    //     void twopole_ctrl_norm(bool bnorm)
    //     {
    //         norm = bnorm;
            
    //         if( norm )
    //         {
    //             // Normalize the filter gain ... not terribly efficient
    //             double real = 1.0 - prad + (m_a2 - prad) * cos( 2.0 * SynthCommon.ONE_PI * pfreq / srate );
    //             double imag = (m_a2 - prad) * sin( 2.0 * SynthCommon.ONE_PI * pfreq / srate );
    //             m_b0 = sqrt( real*real + imag*imag );
    //         }
    //     }
    // }

    // ////////////////////////////////////////////////////////////////////////////////////////////
    // ////////////////////////////////////////////////////////////////////////////////////////////
    // ////////////////////////////////////////////////////////////////////////////////////////////
    // public class twozero : Filter
    // {
    //     public twozero()
    //     {
    //         //...
    //     }

    //     public override int Read(float[] buffer, int offset, int count)
    //     {
    //         throw new System.NotImplementedException();
    //     }

    //     public override void ControlChange(int controlId, object value)
    //     {
    //         throw new System.NotImplementedException();
    //     }

    //     public double Sample(double din)
    //     {
    //         m_input0 = din;
    //         m_output0 = m_b0 * m_input0 + m_b1 * m_input1 + m_b2 * m_input2;
    //         m_input2 = m_input1;
    //         m_input1 = m_input0;

    //         return m_output0;
    //     }

    //     public double Freq
    //     {
    //         get { return zfreq; }
    //         set { twozero_ctrl_freq(value); }
    //     }

    //     public double Rad
    //     {
    //         get { return zrad; }
    //         set { twozero_ctrl_rad(value); }
    //     }


    //     void twozero_ctrl_freq(double freq)
    //     {
    //         zfreq = freq;
    //         SetNotch();
            
    //         // normalize the filter gain TODOX make function
    //         if( m_b1 > 0.0 )
    //             m_b0 = 1.0 / (1.0 + m_b1 + m_b2);
    //         else
    //             m_b0 = 1.0 / (1.0 - m_b1 + m_b2);
    //         m_b1 *= m_b0;
    //         m_b2 *= m_b0;
    //     }

    //     void twozero_ctrl_rad(double rad)
    //     {
    //         zrad = rad;
    //         SetNotch();

    //         // normalize the filter gain
    //         if( m_b1 > 0.0f )
    //             m_b0 = 1.0f / (1.0f + m_b1 + m_b2);
    //         else
    //             m_b0 = 1.0f / (1.0f - m_b1 + m_b2);
    //         m_b1 *= m_b0;
    //         m_b2 *= m_b0;    
    //     }
    // }
}
