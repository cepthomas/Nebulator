
using System;
using System.Collections.Generic;
using Nebulator.Device;


namespace Nebulator.Synth
{
    public class ADSR : UGen
    {
        #region Fields
        /// <summary>Where we are on the profile.</summary>
        enum ADSRState { Idle, Attack, Decay, Sustain, Release }
        ADSRState _state = ADSRState.Idle;

        /// <summary>How much to change output per Next().</summary>
        double _step = 0; // 

        /// <summary>Current output.</summary>
        double _level = 0.0;
        #endregion

        #region Properties
        /// <summary>Time in seconds.</summary>
        public double AttackTime { get; set; } = 0.1;

        /// <summary>Time in seconds.</summary>
        public double DecayTime { get; set; } = 0.25;

        /// <summary>Normalized value.</summary>
        public double SustainLevel { get; set; } = 0.5;
        
        /// <summary>Time in seconds.</summary>
        public double ReleaseTime { get; set; } = 0.25;
        
        /// <summary>Max value.</summary>
        public double Amplitude { get; set; } = 1.0;
        #endregion

        #region Public Functions
        /// <inheritdoc />
        public override double Next(double din = 1)
        {
            switch (_state)
            {
                case ADSRState.Attack:
                    _level += _step;
                    if (_level >= Amplitude)
                    {
                        _level = Amplitude;
                        _state = ADSRState.Decay;
                        _step = (_level - SustainLevel) / (DecayTime * SynthCommon.SampleRate);
                    }
                    break;

                case ADSRState.Decay:
                    _level -= _step;

                    if (_level <= SustainLevel)
                    {
                        _level = SustainLevel;
                        _state = ADSRState.Sustain;
                    }
                    break;

                case ADSRState.Sustain:
                    break;

                case ADSRState.Release:
                    _level -= _step;
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

        /// <summary>
        /// Start the envelope.
        /// </summary>
        public void KeyDown()
        {
            _level = 0;
            _state = ADSRState.Attack;
            _step = Amplitude / (AttackTime * SynthCommon.SampleRate);

            //Trace("ADSR", $"KeyDown() {_step}");
        }

        /// <summary>
        /// Finish the envelope and go to release.
        /// </summary>
        public void KeyUp()
        {
            _state = ADSRState.Release;
            //_step = (Amplitude - _level) / (ReleaseTime * SynthCommon.SampleRate);
            _step = _level / (ReleaseTime * SynthCommon.SampleRate);

            //Trace("ADSR", $"KeyUp() {_step}");
        }
        #endregion
    }

    public class Mix : UGen
    {
        #region Public Functions
        /// <summary>
        /// Vararg list of inputs to sum.
        /// </summary>
        /// <param name="din"></param>
        /// <returns></returns>
        public double Next(params double[] din)
        {
            double dout = 0;
            for (int i = 0; i < din.Length; i++)
            {
                dout += din[i];
            }
            return dout * Volume;
        }

        /// <inheritdoc />
        public override double Next(double din)
        {
            return din;
        }
        #endregion
    }

    public class Pan : UGen
    {
        #region Properties
        /// <summary>
        /// Goes from -1 (left) to +1 (right).
        /// </summary>
        public double Location { get; set; } = 0.0;
        #endregion

        #region Public Functions
        /// <inheritdoc />
        public Sample Next(double din)
        {
            //Sample dout = new Sample
            //{
            //    Left = din * (1 - Location) / 2,
            //    Right = din * (1 + Location) / 2
            //};

            Sample dout;
            dout.Left = din * Volume * (1 - Location) / 2;
            dout.Right = din * Volume * (1 + Location) / 2;


            return dout;
        }
        #endregion
    }
}