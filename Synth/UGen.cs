using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;


namespace Nebulator.Synth
{
    public class UGen
    {
        #region Properties
        /// <summary>0.0 to 1.0.</summary>
        public double Volume { get; set; } = 0.2;
        #endregion

        #region Public Functions - virtual
        /// <summary>
        /// Process 0 input value. Output is mono.
        /// </summary>
        /// <returns></returns>
        public virtual double Next()
        {
            throw new NotImplementedException("Base Next() called");
        }

        /// <summary>
        /// Process 1 input value. Output is mono.
        /// </summary>
        /// <param name="din">Input data - implementation specific.</param>
        /// <returns></returns>
        public virtual double Next(double din)
        {
            throw new NotImplementedException("Base Next() called");
        }

        /// <summary>
        /// Process 1 input sample. Output is stereo.
        /// </summary>
        /// <param name="din"></param>
        /// <returns></returns>
        public virtual Sample Next2(Sample din)
        {
            throw new NotImplementedException("Base Next() called");
        }

        /// <summary>
        /// Process 0 input value. Output is stereo.
        /// </summary>
        /// <returns></returns>
        public virtual Sample Next2()
        {
            throw new NotImplementedException("Base Next() called");
        }

        /// <summary>
        /// Process 1 input value. Output is stereo.
        /// </summary>
        /// <param name="din">Input data - implementation specific.</param>
        /// <returns></returns>
        public virtual Sample Next2(double din)
        {
            throw new NotImplementedException("Base Next() called");
        }

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
        /// Thou shall be no more than 1.0 nor less than 0.0.
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

        /// <summary>
        /// Rudimentary debug helper.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="msg"></param>
        protected void Trace(string type, string msg)
        {
            Console.WriteLine($"SYN {type} {msg}");
        }
        #endregion
    }
}
