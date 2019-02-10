using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;


namespace Nebulator.Synth
{
    public abstract class UGen
    {
        #region Fields

        #endregion

        #region Properties
        /// <summary>0.0 to 1.0.</summary>
        public double Volume { get; set; } = 0.2;
        #endregion

        #region Lifecycle
        #endregion

        #region Public Functions - virtual
        /// <summary>
        /// Process one input sample. Output is mono.
        /// </summary>
        /// <param name="din">Input data - implementation specific.</param>
        /// <returns></returns>
        public abstract double Next(double din);

        /// <param name="noteNumber">Which note to start</param>
        /// <param name="amplitude">Normalized</param>
        public virtual void NoteOn(double noteNumber, double amplitude)
        {
            // Non-implementers can ignore.
        }

        /// <param name="noteNumber">Which note to stop</param>
        public virtual void NoteOff(double noteNumber)
        {
            // Non-implementers can ignore.
        }

        /// <summary>
        /// Implementation specific.
        /// </summary>
        public virtual void Reset()
        {
            // Non-implementers can ignore.
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Convert float note to frequency.
        /// </summary>
        /// <param name="noteNumber"></param>
        /// <returns></returns>
        protected double NoteToFreq(double noteNumber)
        {
            double frequency = 220.0 * Math.Pow(2.0, (noteNumber - 57.0) / 12.0);
            return frequency;
        }

        /// <summary>
        /// Thy shall be no more than 1.0 nor less than 0.0.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        protected double Bound(double val)
        {
            return Math.Max(0.0, Math.Min(1.0, val));
        }

        /// <summary>
        /// Be within 0.0 and 1.0 by wrapping.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        protected double Wrap(double val)
        {
            return (val >= 1.0) || (val <= -1.0) ? val - Math.Floor(val) : val;
        }
        #endregion
    }

    public abstract class UGen2
    {
        // Stereo output version of UGen.

        #region Properties
        /// <summary>0.0 to 1.0.</summary>
        public double Volume { get; set; } = 0.2;
        #endregion

        #region Public Functions - virtual
        /// <summary>
        /// Process one input sample. Output is stereo.
        /// </summary>
        /// <param name="din">Input data - implementation specific.</param>
        /// <returns></returns>
        public abstract Sample Next(double din);

        /// <param name="noteNumber">Which note to start.</param>
        /// <param name="amplitude">Normalized</param>
        public virtual void NoteOn(double noteNumber, double amplitude)
        {
            // Non-implementers can ignore.
        }

        /// <param name="noteNumber">Which note to stop.</param>
        public virtual void NoteOff(double noteNumber)
        {
            // Non-implementers can ignore.
        }
        #endregion
    }
}
