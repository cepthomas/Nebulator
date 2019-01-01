using System;


namespace Nebulator.Synth
{
    /// <summary>
    /// Oscillator base class.
    /// </summary>
    public class Osc : UGen
    {
        //-----------------------------------------------------------------------------
        // basic/default/baseclass osc is a phasor/ramp... 
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
        // simple ramp generator ( 0 to 1 ) this can be fed into other oscillators ( with sync mode of 2 ) as a phase control.
        // see examples/sixty.ck for an example
        // .freq  (float, READ/WRITE)  oscillator frequency (Hz)
        // .sfreq  (float, READ/WRITE)  oscillator frequency (Hz), phase-matched
        // .phase  (float, READ/WRITE)  current phase
        // .sync  (int, READ/WRITE)  (0) sync frequency to input, (1) sync phase to input, (2) fm synth
        // .width  (float, READ/WRITE)  set duration of the ramp in each cycle. (default 1.0)
        //
        // SinOsc s => dac;
        // 440 => s.freq;
        // 1::second => now;

        public enum SyncType
        {
            Freq = 0,   // synch frequency to input // the input signal will set the frequency of the oscillator
            Phase = 1,  // synch phase to input // the input signal will set the phase of the oscillator
            FM = 2      // the input signal will add to the oscillator's current frequency
        }

        #region Fields
        protected double _phaseIncr = 0.0; // phase increment was _num
        protected double _freq = 0.0; // 0.0 means not playing
        protected double _ampl = 0.0;
        protected SyncType _sync = SyncType.Freq;
        protected double _width = 0.5;
        protected double _phase = 0.0;
        #endregion

        #region Properties
        #endregion

        #region Lifecycle
        public Osc()
        {
        }
        #endregion

        #region Public Functions
        // // TODOX handle all these at the level up I think
        // // Start a note with the given frequency and amplitude.
        // public override void NoteOn(double noteNumber, double amplitude)
        // {
        //     _ampl = amplitude;
        //     Freq = SynthCommon.NoteToFreq(noteNumber);
        // }

        // // Stop a note with the given amplitude (speed of decay).
        // public override void NoteOff(double noteNumber, double amplitude = 0.0)
        // {
        //     _ampl = 0.0;
        //     Freq = 0.0;
        // }

        public override void Reset()
        {
            _ampl = 0.0;
            Freq = 0.0;

        }
        #endregion

        #region Private functions
        #endregion

        public UGen SyncWith { get; set; } = null; // TODOX

        public double Freq
        {
            get
            {
                return _freq;
            }
            set
            {
                _freq = value;
                _phaseIncr = _freq / SynthCommon.SAMPLE_RATE;
                //bound 0.0 -> 1.0
                if (_phaseIncr >= 1.0)
                {
                    _phaseIncr -= Math.Floor(_phaseIncr);
                }
            }
        }

        // desc: set oscillator phase wrapped to ( 0 - 1 )
        public double Phase
        {
            get
            {
                return _phase;
            }
            set
            {
                _phase = value;
                //bound 0.0 -> 1.0
                if (_phase >= 1.0 || _phase < 0.0)
                {
                    _phase -= Math.Floor(_phaseIncr);
                }
            }
        }

        // desc: set width of active phase ( bound 0.0 - 1.0 );
        public double Width
        {
            get
            {
                return _width;
            }
            set
            {
                _width = value;
                //bound 0.0 -> 1.0
                _width = Math.Max(0.0, Math.Min(1.0, _width));
            }
        }

        public SyncType Sync
        {
            get
            {
                return _sync;
            }
            set
            {
                // set sync
                SyncType psync = _sync;
                _sync = value;

                if (_sync == SyncType.Freq && psync != _sync)
                {
                    // if we are switching to internal tick
                    // we need to pre-advance the phase... 
                    // this is probably stupid.  -pld
                    _phase += _phaseIncr;

                    //bound 0.0 -> 1.0
                    if (_phase > 1.0)
                    {
                        _phase -= 1.0; //<<<<<<<<<<<<<<<
                    }
                }
            }
        }


        public override double Next(double din)    // TODOX these overrides are sloppy - can be combined I think. Plus early return.
        {
            if (_freq == 0.0)
            {
                return 0;
            }

            bool incrementPhase = true;

            switch(_sync)
            {
                case SyncType.Freq:
                    _freq = din;
                    _phaseIncr = _freq / SynthCommon.SAMPLE_RATE;
                    //bound 0.0 -> 1.0
                    if (_phaseIncr >= 1.0 || _phaseIncr < 0.0)
                    {
                        _phaseIncr -= Math.Floor(_phaseIncr);
                    }
                    break;

                case SyncType.Phase:
                    _phase = din;
                    incrementPhase = false;
                    // bound it (thanks Pyry) //bound 0.0 -> 1.0 or should it be -=1.0 like above?
                    if (_phase > 1.0 || _phase < 0.0)
                    {
                        _phase -= Math.Floor(_phase);
                    }
                    break;

                case SyncType.FM:
                    double freq = _freq + din;
                    _phaseIncr = freq / SynthCommon.SAMPLE_RATE;
                    //bound 0.0 -> 1.0
                    if (_phaseIncr >= 1.0 || _phaseIncr < 0.0)
                    {
                        _phaseIncr -= Math.Floor(_phaseIncr);
                    }
                    break;
            }

            ///// set output to current phase /////
            double dout = _phase;

            if (incrementPhase)
            {
                _phase += _phaseIncr;
                //bound 0.0 -> 1.0  phase wraps around
                if (_phase > 1.0)
                {
                    _phase -= 1.0;
                }
                else if (_phase < 0.0)
                {
                    _phase += 1.0;
                }
            }

            return dout * Gain;
        }


        // public double Period
        // {
        //     get
        //     {
        //         double period = 0;

        //         // get period
        //         if (_freq != 0.0)
        //         {
        //             period = 1 / _freq * SynthCommon.SAMPLE_RATE;
        //         }

        //         // return
        //         return period;
        //     }
        //     set
        //     {
        //         // test
        //         if (value == 0.0)
        //         {
        //             _freq = 0.0;
        //         }
        //         else
        //         {
        //             _freq = 1 / (value / SynthCommon.SAMPLE_RATE);
        //         }

        //         _phaseIncr = _freq / SynthCommon.SAMPLE_RATE;

        //         // bound it
        //         if (_phaseIncr >= 1.0)
        //         {
        //             _phaseIncr -= Math.Floor(_phaseIncr);
        //         }
        //     }
        // }

    }

    public class SinOsc : Osc
    {
        public override double Next(double din)
        {
            if (_freq == 0.0)
            {
                return 0;
            }

            bool incrementPhase = true;

            switch(_sync)
            {
                case SyncType.Freq:
                    _freq = din;
                    _phaseIncr = _freq / SynthCommon.SAMPLE_RATE;
                    // bound it
                    if (_phaseIncr >= 1.0)
                    {
                        _phaseIncr -= Math.Floor(_phaseIncr);
                    }
                    else if (_phaseIncr <= -1.0)
                    {
                        _phaseIncr += Math.Floor(_phaseIncr);
                    }
                    break;

                case SyncType.Phase:
                    _phase = din;
                    incrementPhase = false;
                    break;

                case SyncType.FM:
                    double freq = _freq + din;
                    _phaseIncr = freq / SynthCommon.SAMPLE_RATE;
                    // bound it
                    if (_phaseIncr >= 1.0)
                    {
                        _phaseIncr -= Math.Floor(_phaseIncr);
                    }
                    else if (_phaseIncr <= -1.0)
                    {
                        _phaseIncr += Math.Floor(_phaseIncr);
                    }
                    break;
            }

            ///// set output /////
            double dout = Math.Sin(_phase * SynthCommon.TWO_PI);

            if (incrementPhase)
            {
                _phase += _phaseIncr;
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

            return dout * Gain;
        }
    }

    public class TriOsc : Osc
    {
        public override double Next(double din)
        {
            if (_freq == 0.0)
            {
                return 0;
            }

            bool incrementPhase = true;

            switch(_sync)
            {
                case SyncType.Freq:
                    // set freq
                    _freq = din;
                    // phase increment
                    _phaseIncr = _freq / SynthCommon.SAMPLE_RATE;

                    // bound it
                    if (_phaseIncr >= 1.0)
                    {
                        _phaseIncr -= Math.Floor(_phaseIncr);
                    }
                    else if (_phaseIncr <= -1.0)
                    {
                        _phaseIncr += Math.Floor(_phaseIncr);
                    }
                    break;

                case SyncType.Phase:
                    // set freq
                    _phase = din;
                    incrementPhase = false;
                    break;

                case SyncType.FM:
                    // set freq
                    double freq = _freq + din;
                    // phase increment
                    _phaseIncr = freq / SynthCommon.SAMPLE_RATE;

                    // bound it
                    if (_phaseIncr >= 1.0)
                    {
                        _phaseIncr -= Math.Floor(_phaseIncr);
                    }
                    else if (_phaseIncr <= -1.0)
                    {
                        _phaseIncr += Math.Floor(_phaseIncr);
                    }
                    break;
            }

            ///// compute /////
            double dout = 0.0;

            double phase = _phase + .25;

            if (phase > 1.0)
            {
                phase -= 1.0;
            }

            if (phase < _width)
            {
                dout = (_width == 0.0) ? 1.0 : -1.0 + 2.0 * phase / _width; 
            }
            else
            {
                dout = (_width == 1.0) ? 0 : 1.0 - 2.0 * (phase - _width) / (1.0 - _width);
            }

            // advance internal phase
            if (incrementPhase)
            {
                _phase += _phaseIncr;
                // bound
                if (_phase > 1.0)
                {
                    _phase -= 1.0;
                }
                else if (_phase < 0.0)
                {
                    _phase += 1.0;
                }
            }

            return dout * Gain;
        }
    }

    public class PulseOsc : Osc
    {
        public override double Next(double din)
        {
            if (_freq == 0.0)
            {
                return 0;
            }

            bool incrementPhase = true;

            switch(_sync)
            {
                case SyncType.Freq:
                    _freq = din;
                    _phaseIncr = _freq / SynthCommon.SAMPLE_RATE;
                    // bound it
                    if (_phaseIncr >= 1.0)
                    {
                        _phaseIncr -= Math.Floor(_phaseIncr);
                    }
                    else if (_phaseIncr <= -1.0)
                    {
                        _phaseIncr += Math.Floor(_phaseIncr);
                    }
                    break;

                case SyncType.Phase:
                    _phase = din;
                    incrementPhase = false;
                    break;

                case SyncType.FM:
                    double freq = _freq + din;
                    _phaseIncr = freq / SynthCommon.SAMPLE_RATE;
                    // bound it
                    if (_phaseIncr >= 1.0)
                    {
                        _phaseIncr -= Math.Floor(_phaseIncr);
                    }
                    else if (_phaseIncr <= -1.0)
                    {
                        _phaseIncr += Math.Floor(_phaseIncr);
                    }
                    break;
            }

            ///// compute /////
            double dout = (_phase < _width) ? 1.0 : -1.0;

            // move phase
            if (incrementPhase)
            {
                _phase += _phaseIncr;
                // bound
                if (_phase > 1.0)
                {
                    _phase -= 1.0;
                }
                else if (_phase < 0.0)
                {
                    _phase += 1.0;
                }
            }

            return dout * Gain;
        }
    }
}
