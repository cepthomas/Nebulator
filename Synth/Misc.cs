
using System;
using System.Collections.Generic;

namespace Nebulator.Synth
{
    public class ADSR : UGen
    {
        #region Fields
        // private enum
        enum ADSRState { Idle, Attack, Decay, Sustain, Release }
        ADSRState _state = ADSRState.Idle;
        double _step = 0; // how much to change output per Next().
        double _level = 0.0;
        bool _released = false;
        #endregion

        #region Properties
        // make all times seconds
        public double AttackTime { get; set; } = 0.1;
        public double DecayTime { get; set; } = 0.25;
        public double SustainLevel { get; set; } = 0.5;
        public double ReleaseTime { get; set; } = 0.25;
        public double Amplitude { get; set; } = 1.0;
        #endregion

        #region Lifecycle
        #endregion

        #region Public Functions
        public override double Next(double din)
        {
            // If note is released, go directly to Release state, except if still attacking.
            if (_released && (_state == ADSRState.Decay || _state == ADSRState.Sustain))
            {
                _state = ADSRState.Release;
                _step = -((Amplitude - _level) / (ReleaseTime * SynthCommon.SampleRate));
            }

            switch (_state)
            {
                case ADSRState.Attack:
                    _level += _step;
                    if (_level >= Amplitude)
                    {
                        _level = Amplitude;
                        _state = ADSRState.Decay;
                        _step = -((_level - SustainLevel) / (DecayTime * SynthCommon.SampleRate));
                    }
                    _released = false;
                    break;

                case ADSRState.Decay:
                    _level += _step;

                    if (_level <= SustainLevel)
                    {
                        _level = SustainLevel;
                        _state = ADSRState.Sustain;
                    }
                    break;

                case ADSRState.Sustain:
                    break;

                case ADSRState.Release:
                    _level += _step;
                    if (_level <= 0)
                    {
                        _level = 0;
                        _state = ADSRState.Idle;
                    }
                    break;

                case ADSRState.Idle:
                    break;
            }

            return _level * din;
        }

        public void Key(bool on)
        {
            if(on)
            {
                _level = 0;
                _state = ADSRState.Attack;
                _step = Amplitude / AttackTime / SynthCommon.SampleRate;
            }
            else
            {
                _released = true;
            }
        }
        #endregion

        #region Private functions
        #endregion
    }

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
        public double Next(params double[] din)
        {
            double dout = 0;
            for (int i = 0; i < din.Length; i++)
            {
                dout += din[i];
            }
            return dout * Gain;
        }

        public override double Next(double din)
        {
            return din;
        }
        #endregion

        #region Private functions
        #endregion
    }

    public class Pan : UGen2
    {
        #region Fields
        #endregion

        #region Properties
        // -1 to +1
        public double Location { get; set; } = 0.0;
        #endregion

        #region Lifecycle
        public Pan()
        {
        }
        #endregion

        #region Public Functions
        public override Sample Next(double din)
        {
            Sample dout = new Sample
            {
                Left = din * (1 - Location) / 2,
                Right = din * (1 + Location) / 2
            };
            return dout * Gain;
        }
        #endregion

        #region Private functions
        #endregion
    }
}