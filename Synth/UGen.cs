using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;


namespace Nebulator.Synth
{
    public struct Sample  // TODOX2 - stereo - this or tuple?
    {
        public double Left;
        public double Right;

        public static Sample operator +(Sample t1, Sample t2)
        {
            Sample ret;
            ret.Left = t1.Left + t2.Left;
            ret.Right = t1.Right + t2.Right;
            return ret;
        }

        public static Sample operator *(Sample t1, double d)
        {
            Sample ret;
            ret.Left = t1.Left * d;
            ret.Right = t1.Right * d;
            return ret;
        }
    }

    public abstract class UGen
    {
        #region Fields

        #endregion

        #region Properties
        /// <summary>
        /// 0.0 to 1.0 - gain is applied to input signals firstly.
        /// </summary>
        public double Gain { get; set; } = 0.2;
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
        //public virtual double Next(double din)
        // {
        //     throw new Exception("You unexpectedly called virtual method Next(double din)");
        // }

        /// <param name="noteNumber">Which note</param>
        /// <param name="amplitude">If 0.0, stop, otherwise start - normalized</param>
        public virtual void Note(double noteNumber, double amplitude)
        {
            // Non-implementers can ignore.
        }

        ///// <summary>
        ///// Create a sound with the specified frequency and amplitude.
        ///// Implementers may ignore or interpret as KeyOn().
        ///// </summary>
        ///// <param name="frequency">Which frequency</param>
        ///// <param name="amplitude">If 0.0, stop, otherwise start - normalized</param>
        // public virtual void Sound(double frequency, double amplitude)
        // {
        //     // Non-implementers can ignore.
        // }

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
        /// Be within.
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
        /// <summary>
        /// 0.0 to 1.0 - gain is applied to input signals firstly.
        /// </summary>
        public double Gain { get; set; } = 0.2;
        #endregion

        #region Public Functions - virtual
        /// <summary>
        /// Process one input sample. Output is stereo.
        /// </summary>
        /// <param name="din">Input data - implementation specific.</param>
        /// <returns></returns>
        public abstract Sample Next(double din);
        // public virtual Sample Next(double din)
        // {
        //     throw new Exception("You unexpectedly called virtual method Next2(double din)");
        // }

        /// <summary>
        /// Start or stop a note with the given frequency and amplitude.
        /// Implementers may ignore or interpret as KeyOn().
        /// </summary>
        /// <param name="noteNumber">Which note</param>
        /// <param name="amplitude">If 0.0, stop, otherwise start - normalized</param>
        public virtual void Note(double noteNumber, double amplitude)
        {
            // Non-implementers can ignore.
        }
        #endregion
    }
}
