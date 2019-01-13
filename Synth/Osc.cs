using System;


namespace Nebulator.Synth
{
    // SawOsc is TriOsc with width=0.0 or width=1.0
    // SqrOsc is PulseOsc at width=0.5

    public enum SyncType
    {
        Freq = 0,   // synch frequency to input // the input signal will set the frequency of the oscillator
        Phase = 1,  // synch phase to input // the input signal will set the phase of the oscillator
        FM = 2      // the input signal will add to the oscillator's current frequency - TODON2 see fm2.ck
    }


    /// <summary>
    /// Oscillator base class.
    /// </summary>
    public abstract class Osc : UGen
    {
        #region Fields
        protected double _phaseIncr = 0.0; // phase increment was _num
        protected double _freq = 0.0; // 0.0 means not playing
        protected double _mod = 0.0; // modulation value - FM
        // protected double _ampl = 0.0;  TODON2 stk/chuck uses gain instead of separate amplitude.
        protected SyncType _sync = SyncType.Freq;
        protected double _width = 0.5;
        protected double _phase = 0.0;
        #endregion

        #region Properties
        public SyncType Sync { get; set; }
        //TODON2 ?? if (_sync == SyncType.Freq && psync != _sync)
        //set: 
        //{
        //    // if we are switching to internal tick
        //    // we need to pre-advance the phase... 
        //    // this is probably stupid.  -pld
        //    _phase += _phaseIncr;
        //    //bound 0.0 -> 1.0
        //    if (_phase > 1.0)
        //    {
        //        _phase -= 1.0;
        //    }
        //}

        public double Freq
        {
            get { return _freq; }
            set { _freq = value; _phaseIncr = _freq / SynthCommon.SampleRate; }
        }

        public double Phase
        {
            get { return _phase; }
            set { _phase = Wrap(value); }
        }

        public double Mod
        {
            get { return _mod; }
            set { _mod = value; }
        }

        public double Width
        {
            get { return _width; }
            set { _width = Bound(value); }
        }
        #endregion

        #region Public Functions
        /// <inheritdoc />
        public override void Note(double noteNumber, double amplitude)
        {
            double frequency = NoteToFreq(noteNumber);
            Freq = frequency;
            Gain = amplitude;
        }

        /// <inheritdoc />
        public override double Next(double _) // may use something for input TODON2
        {
            double dout = 0.0;

            if (_freq > 0.0)
            {
                bool incrementPhase = true;

                switch (_sync)
                {
                    case SyncType.Freq: //Base
                        //_freq = din;
                        _phaseIncr = _freq / SynthCommon.SampleRate;
                        break;

                    case SyncType.Phase: //Base
                        //_phase = din;
                        incrementPhase = false;
                        break;

                    case SyncType.FM: //Base
                        //double freq = _freq + din;
                        double freq = _freq + _mod;
                        _phaseIncr = freq / SynthCommon.SampleRate;
                        break;
                }

                // Shape specific calc.
                dout = ComputeNext();

                if (incrementPhase)
                {
                    _phase = Wrap(_phase + _phaseIncr);
                }
            }

            return dout * Gain;
        }

        /// <inheritdoc />
        public override void Reset()
        {
            // _ampl = 0.0;
            Freq = 0.0;
        }
        #endregion

        #region Private functions
        /// <summary>
        /// The shape specific calculation to be provided by implementation.
        /// </summary>
        /// <returns></returns>
        protected abstract double ComputeNext();
        #endregion
    }

    public class Phasor : Osc
    {
        protected override double ComputeNext()
        {
            return _phase;
        }
    }

    public class SinOsc : Osc
    {
        protected override double ComputeNext()
        {
            return Math.Sin(_phase * Math.PI * 2);
        }
    }

    public class TriOsc : Osc
    {
        protected override double ComputeNext()
        {
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
            return dout;
        }
    }

    public class PulseOsc : Osc
    {
        protected override double ComputeNext()
        {
            return (_phase < _width) ? 1.0 : -1.0; ;
        }
    }
}
