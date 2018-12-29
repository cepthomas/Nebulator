using System;


namespace Nebulator.Synth
{
    /// <summary>
    /// Oscillator base class.
    /// </summary>
    public class Osc : UGen
    {
        //// sync frequency to input
        //if (_sync == 0)
        //// sync phase to input
        //else if (_sync == 1)
        //// FM synthesis
        //else if (_sync == 2)
        protected enum SyncType { Freq, Phase, FM }

        #region Fields
        protected double _num; // ticks
        protected double _freq;
        protected SyncType _sync;
        protected double _width;
        protected double _phase;
        #endregion

        #region Properties
        #endregion

        #region Lifecycle
        #endregion

        #region Public Functions
        #endregion

        #region Private functions
        #endregion

        public UGen SyncWith { get; set; } = null;

        public Osc()
        {
            _num = 0.0;
            _freq = 220.0;
            _sync = SyncType.Freq; 
            _width = 0.5;
            _phase = 0.0;

            osc_ctrl_freq(_freq);
        }


        /// Start a note with the given frequency and amplitude.
        public override void NoteOn(double noteNumber, double amplitude)
        {

        }

        /// Stop a note with the given amplitude (speed of decay).
        public override void NoteOff(double noteNumber, double amplitude = 0.0)
        {

        }

        /////
        //public override void Clear()
        //{

        //}

        /// and/or
        public override void Reset()
        {

        }




        //-----------------------------------------------------------------------------
        // name: osc_ctrl_freq()
        // desc: set oscillator frequency
        //-----------------------------------------------------------------------------
        public void osc_ctrl_freq(double freq)
        {
            // set freq
            _freq = freq;
            // phase increment
            _num = _freq / SynthCommon.SAMPLE_RATE;
            // bound it
            if (_num >= 1.0)
            {
                _num -= Math.Floor(_num);
            }
        }

        //-----------------------------------------------------------------------------
        // name: osc_ctrl_period()
        // desc: set oscillator period
        //-----------------------------------------------------------------------------
        public void osc_ctrl_period(double period)
        {
            // test
            if (period == 0.0)
                _freq = 0.0;
            else
                _freq = 1 / (period / SynthCommon.SAMPLE_RATE);
            _num = _freq / SynthCommon.SAMPLE_RATE;
            // bound it
            if (_num >= 1.0)
            {
                _num -= Math.Floor(_num);
            }
            // // return
            // RETURN.v_dur = period;
        }

        //-----------------------------------------------------------------------------
        // name: osc_cget_period()
        // desc: get oscillator period
        //-----------------------------------------------------------------------------
        public double osc_cget_period()
        {
            double period = 0;

            // get period
            if (_freq != 0.0)
            {
                period = 1 / _freq * SynthCommon.SAMPLE_RATE;
            }

            // return
            return period;
        }

        //-----------------------------------------------------------------------------
        // name: osc_ctrl_phase()
        // desc: set oscillator phase wrapped to ( 0 - 1 )
        //-----------------------------------------------------------------------------
        public void osc_ctrl_phase(double phase)
        {
            // set freq
            _phase = phase;
            //bound ( this could be set arbitrarily high or low ) 
            if (_phase >= 1.0 || _phase < 0.0)
            {
                _phase -= Math.Floor(_num);
            }
            // // return
            // RETURN.v_float = (double)phase;
        }

        //-----------------------------------------------------------------------------
        // name: osc_ctrl_width()
        // desc: set width of active phase ( bound 0.0 - 1.0 );
        //-----------------------------------------------------------------------------
        void osc_ctrl_width(double width)
        {
            _width = width;
            //bound ( this could be set arbitrarily high or low ) 
            _width = Math.Max(0.0, Math.Min(1.0, _width));
        }

        //-----------------------------------------------------------------------------
        // name: osc_ctrl_sync()
        // desc: select sync mode for oscillator
        //-----------------------------------------------------------------------------
        void osc_ctrl_sync(SyncType sync)
        {
            // set sync
            SyncType psync = _sync;
            _sync = sync;

            //t_CKINT psync = d->sync;
            //d->sync = GET_CK_INT(ARGS);
            //// bound ( this could be set arbitrarily high or low ) 
            //if (_sync < 0 || _sync > 2)
            //    _sync = 0;

            if (_sync == SyncType.Freq && psync != _sync)
            {
                // if we are switching to internal tick
                // we need to pre-advance the phase... 
                // this is probably stupid.  -pld
                _phase += _num;
                // keep the phase between 0 and 1
                if (_phase > 1.0)
                {
                    _phase -= 1.0;
                }
            }
        }


        //-----------------------------------------------------------------------------
        // basic osc is a phasor... 
        // we use a duty-cycle rep ( 0 - 1 ) rather than angular ( 0 - TWOPI )
        // sinusoidal oscillators are special
        //
        // (maybe) as a rule, we store external phase control values
        // so that we can have a smooth change back to internal control -pld
        //
        // technically this should happen even with external phase control
        // that we'd be in the right place when translating back to internal... 
        // this was decidely inefficient and nit-picky.  -pld 
        //
        //-----------------------------------------------------------------------------
        public override double Sample(double din) // TODOX these overrides are sloppy
        {
            // get the data
            // Osc_Data* d = (Osc_Data*)OBJ_MEMBER_UINT(SELF, osc_offset_data);
            // Chuck_UGen* ugen = (Chuck_UGen*)SELF;
            bool inc_phase = true;

            // if input
            // if (ugen.m_num_src)
            {
                // sync frequency to input
                if (_sync == SyncType.Freq)
                {
                    // set freq
                    _freq = din;
                    // phase increment
                    _num = _freq / SynthCommon.SAMPLE_RATE;
                    // bound it
                    if (_num >= 1.0 || _num < 0.0)
                    {
                        _num -= Math.Floor(_num);
                    }
                }
                // synch phase to input
                else if (_sync == SyncType.Phase)
                {
                    // set phase
                    _phase = din;
                    // no update
                    inc_phase = false;
                    // bound it (thanks Pyry)
                    if (_phase > 1.0 || _phase < 0.0)
                    {
                        _phase -= Math.Floor(_phase);
                    }
                }
                // fm synthesis
                else if (_sync == SyncType.FM)
                {
                    // set freq
                    double freq = _freq + din;
                    // phase increment
                    _num = freq / SynthCommon.SAMPLE_RATE;
                    // bound it
                    if (_num >= 1.0 || _num < 0.0)
                    {
                        _num -= Math.Floor(_num);
                    }
                }
                // sync to now
                // else if( sync == 3 )
                // {
                //     phase = now * num;
                //     inc_phase = false;
                // }
            }

            // set output to current phase
            //*out = (SAMPLE)phase;
            double dout = _phase;

            // check
            if (inc_phase)
            {
                // step the phase.
                _phase += _num;
                // keep the phase between 0 and 1
                if (_phase > 1.0)
                {
                    _phase -= 1.0;
                }
                else if (_phase < 0.0)
                {
                    _phase += 1.0;
                }
            }

            return dout;
        }
    }

    public class SinOsc : Osc
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

        public override double Sample(double din)
        {
            // get the data
            // Osc_Data* d = (Osc_Data*)OBJ_MEMBER_UINT(SELF, osc_offset_data);
            // Chuck_UGen* ugen = (Chuck_UGen*)SELF;
            bool inc_phase = true;

            // if input
//            if (ugen.m_num_src)
            {
                // sync frequency to input
                if (_sync == SyncType.Freq)
                {
                    // set freq
                    _freq = din;
                    // phase increment
                    _num = _freq / SynthCommon.SAMPLE_RATE;
                    // bound it
                    if (_num >= 1.0)
                    {
                        _num -= Math.Floor(_num);
                    }
                    else if (_num <= -1.0)
                    {
                        _num += Math.Floor(_num);
                    }
                }
                // sync phase to input
                else if (_sync == SyncType.Phase)
                {
                    // set freq
                    _phase = din;
                    inc_phase = false;
                }
                // FM synthesis
                else if (_sync == SyncType.FM)
                {
                    // set freq
                    double freq = _freq + din;
                    // phase increment
                    _num = freq / SynthCommon.SAMPLE_RATE;
                    // bound it
                    if (_num >= 1.0)
                    {
                        _num -= Math.Floor(_num);
                    }
                    else if (_num <= -1.0)
                    {
                        _num += Math.Floor(_num);
                    }
                }
                // sync phase to now
                // else if( sync == 3 )
                // {
                //     phase = now * num;
                //     inc_phase = false;
                // }
            }

            // set output
            //*out = (SAMPLE) ::sin(phase * TWO_PI);
            double dout = Math.Sin(_phase * SynthCommon.TWO_PI);

            if (inc_phase)
            {
                // next phase
                _phase += _num;
                // keep the phase between 0 and 1
                if (_phase > 1.0)
                {
                    _phase -= 1.0;
                }
                else if (_phase < 0.0)
                {
                    _phase += 1.0;
                }
            }

            return dout;
        }
    }

    public class TriOsc : Osc
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

        public override double Sample(double din)
        {
            // get the data
            // Osc_Data* d = (Osc_Data*)OBJ_MEMBER_UINT(SELF, osc_offset_data);
            // Chuck_UGen* ugen = (Chuck_UGen*)SELF;
            bool inc_phase = true;

            // if input
 //           if (ugen.m_num_src)
            {
                // sync frequency to input
                if (_sync == SyncType.Freq)
                {
                    // set freq
                    _freq = din;
                    // phase increment
                    _num = _freq / SynthCommon.SAMPLE_RATE;
                    // bound it
                    if (_num >= 1.0)
                    {
                        _num -= Math.Floor(_num);
                    }
                    else if (_num <= -1.0)
                    {
                        _num += Math.Floor(_num);
                    }
                }
                // sync phase to input
                else if (_sync == SyncType.Phase)
                {
                    // set freq
                    _phase = din;
                    inc_phase = false;
                }
                // FM synthesis
                else if (_sync == SyncType.FM)
                {
                    // set freq
                    double freq = _freq + din;
                    // phase increment
                    _num = freq / SynthCommon.SAMPLE_RATE;
                    // bound it
                    if (_num >= 1.0)
                    {
                        _num -= Math.Floor(_num);
                    }
                    else if (_num <= -1.0)
                    {
                        _num += Math.Floor(_num);
                    }
                }
                // sync to now
                // if( sync == 3 )
                // {
                //     phase = now * num;
                //     inc_phase = false;
                // }
            }

            // compute
            double phase = _phase + .25;
            if (phase > 1.0)
            {
                phase -= 1.0;
            }
            //if (phase < width)
            //    *out = (SAMPLE)(width == 0.0) ? 1.0 : -1.0 + 2.0 * phase / width; 
            //else
            //    *out = (SAMPLE)(width == 1.0) ? 0 : 1.0 - 2.0 * (phase - width) / (1.0 - width);

            double dout;
            if (phase < _width)
                dout = (_width == 0.0) ? 1.0 : -1.0 + 2.0 * phase / _width; 
            else
                dout = (_width == 1.0) ? 0 : 1.0 - 2.0 * (phase - _width) / (1.0 - _width);

            // advance internal phase
            if (inc_phase)
            {
                _phase += _num;
                // keep the phase between 0 and 1
                if (_phase > 1.0)
                {
                    _phase -= 1.0;
                }
                else if (_phase < 0.0)
                {
                    _phase += 1.0;
                }
            }

            return dout;
        }
    }

    public class PulseOsc : Osc
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

        public override double Sample(double din)
        {
            // get the data
            // Osc_Data* d = (Osc_Data*)OBJ_MEMBER_UINT(SELF, osc_offset_data);
            // Chuck_UGen* ugen = (Chuck_UGen*)SELF;
            bool inc_phase = true;

            // if input
 //           if (ugen.m_num_src)
            {
                // sync frequency to input
                if (_sync == SyncType.Freq)
                {
                    // set freq
                    _freq = din;
                    // phase increment
                    _num = _freq / SynthCommon.SAMPLE_RATE;
                    // bound it
                    if (_num >= 1.0)
                    {
                        _num -= Math.Floor(_num);
                    }
                    else if (_num <= -1.0)
                    {
                        _num += Math.Floor(_num);
                    }
                }
                // sync phase to input
                else if (_sync == SyncType.Phase)
                {
                    // set freq
                    _phase = din;
                    inc_phase = false;
                }
                // FM synthesis
                else if (_sync == SyncType.FM)
                {
                    // set freq
                    double freq = _freq + din;
                    // phase increment
                    _num = freq / SynthCommon.SAMPLE_RATE;
                    // bound it
                    if (_num >= 1.0)
                    {
                        _num -= Math.Floor(_num);
                    }
                    else if (_num <= -1.0)
                    {
                        _num += Math.Floor(_num);
                    }
                }
                // sync to now
                // if( sync == 3 )
                // {
                //     phase = now * num;
                //     inc_phase = false;
                // }
            }

            // compute
            //*out = (SAMPLE)(phase < width) ? 1.0 : -1.0;
            double dout = (_phase < _width) ? 1.0 : -1.0;

            // move phase
            if (inc_phase)
            {
                _phase += _num;
                // keep the phase between 0 and 1
                if (_phase > 1.0)
                {
                    _phase -= 1.0;
                }
                else if (_phase < 0.0)
                {
                    _phase += 1.0;
                }
            }

            return dout;
        }
    }

    //////////////// sawosc_tick is tri_osc tick with width=0.0 or width=1.0  -pld 
    public class SawOsc : Osc
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

        public SawOsc()
        {
            sawosc_ctrl_width(_width);
        }

        //-----------------------------------------------------------------------------
        // name: sawosc_ctrl_width()
        // force width to 0.0 ( falling ) or 1.0 ( rising ) 
        //-----------------------------------------------------------------------------
        public void sawosc_ctrl_width(double width)
        {
            // set freq
            _width = width;
            // bound ( this could be set arbitrarily high or low ) 
            _width = (_width < 0.5) ? 0.0 : 1.0;  //rising or falling
        }
    }

    ///////// sqrosc_tick is pulseosc_tick at width=0.5 -pld;
    public class SqrOsc : Osc
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
        
        public SqrOsc()
        {
            sqrosc_ctrl_width(_width);
        }

        //-----------------------------------------------------------------------------
        // name: sqrosc_ctrl_width()
        // desc: force width to 0.5;
        //-----------------------------------------------------------------------------
        public void sqrosc_ctrl_width(double width)
        {
            // force value
            _width = 0.5;
        }
    }
}
