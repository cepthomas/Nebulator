
using System;
using System.Collections.Generic;

namespace Nebulator.Synth
{

    
    public class ADSR : UGen
    {
        #region Fields
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
        #endregion

        #region Private functions
        #endregion






        public override double Next(double _)
        {
            // If note is released, go directly to Release state, except if still attacking.
            if (_released && (_state == ADSRState.Decay || _state == ADSRState.Sustain))
            {
                _state = ADSRState.Release;
                _step = -((Amplitude - _level) / (ReleaseTime * SynthCommon.SAMPLE_RATE));
            }

            switch (_state)
            {
                case ADSRState.Attack:
                    _level += _step;
                    if (_level >= Amplitude)
                    {
                        _level = Amplitude;
                        _state = ADSRState.Decay;
                        _step = -((_level - SustainLevel) / (DecayTime * SynthCommon.SAMPLE_RATE));
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

            return _level;
        }

        public void KeyOn()
        {
            _level = 0;
            _state = ADSRState.Attack;
            _step = Amplitude / (AttackTime * SynthCommon.SAMPLE_RATE);
        }

        public void KeyOff()
        {
            _released = true;
        }
    }


    ///////////////////////////////////////////////////////////////////////
    public class ADSR_Sanford : UGen
    {
        #region Fields
        // Coefficients for calculating the output.
        double _a0 = 0.0;
        double _b1 = 0.0;
        // The current output.
        double _output = 0.0;
        enum ADSRState { Idle, Attack, Decay, Sustain, Release }
        ADSRState _state = ADSRState.Idle;
        //What are these?
        // Used as a scaler value to ensure output is in the range of [0, 1].
        const double CEILING = 0.63212;
        // Used as a scaler to scale up time parameters.
        const int TIME_SCALER = 3;
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
        #endregion

        #region Private functions
        #endregion

        public override double Next(double _)
        {
            switch (_state)
            {
                case ADSRState.Attack:
                    _output = _a0 + _b1 * _output;

                    // If the end of the attack segment has been reached.
                    if (_output >= CEILING + 1)
                    {
                        // Calculate coefficients for decay segment.
                        double d = DecayTime * TIME_SCALER * SynthCommon.SAMPLE_RATE + 1;
                        double x = Math.Exp(-1 / d);

                        _a0 = 1 - x;
                        _b1 = x;

                        _output = CEILING + 1;

                        // Enter decay segment.
                        _state = ADSRState.Decay;
                    }
                    break;

                case ADSRState.Decay:
                    _output = _a0 * SustainLevel + _b1 * _output;
                    break;

                case ADSRState.Release:
                    _output = _a0 + _b1 * _output;

                    // If the end of the release segment has been reached.
                    if (_output < 1)
                    {
                        _output = 1;
                        _state = ADSRState.Idle;
                    }
                    break;

                case ADSRState.Idle:
                    break;

                default:
                    //Debug.Fail("Unhandled state");
                    break;
            }

            double o = (_output - 1) / CEILING * Amplitude;
            return o;
        }

        public void KeyOn()
        {
            double d = AttackTime * TIME_SCALER * SynthCommon.SAMPLE_RATE + 1;
            double x = Math.Exp(-1 / (AttackTime * TIME_SCALER * SynthCommon.SAMPLE_RATE));

            _a0 = (1 - x) * 2;
            _b1 = x;

            //_amplitude = Math.Pow((1 - VelocitySensitivity) + velocity * VelocitySensitivity, 2);

            _state = ADSRState.Attack;
        }

        public void KeyOff()
        {
            double d = ReleaseTime * TIME_SCALER * SynthCommon.SAMPLE_RATE + 1;
            double x = Math.Exp(-1 / d);

            _a0 = (1 - x) * 0.9;
            _b1 = x;

            _state = ADSRState.Release;
        }
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
                Left = din * -Location,
                Right = din * Location
            };
            return dout * Gain;
        }
        #endregion

        #region Private functions
        #endregion
    }
}