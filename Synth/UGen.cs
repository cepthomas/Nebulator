using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;


namespace Nebulator.Synth
{
    public struct Sample  //TODOX this or tuple?
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
        // /// <summary></summary>
        // public int NumInputs { get; } = 0;

        ///// <summary></summary>
        //public int NumOutputs { get; } = 0;

        /// <summary>
        /// 0.0 to 1.0 - gain is applied to input signals firstly.
        /// </summary>
        public double Gain { get; set; } = 0.2;

        #endregion

        #region Lifecycle
        protected UGen()
        {
        }
        #endregion

        #region Public Functions - virtual
        /// <summary>
        /// UGens can implement either or both of the two flavors.
        /// Process one input sample. Output is mono.
        /// Oscillators can consider input a control voltage.
        /// </summary>
        /// <param name="din"></param>
        /// <returns></returns>
        public virtual double Next(double din)
        {
            throw new Exception("You called virtual method Next()");
        }

        /// <summary>
        /// UGens can implement either or both of the two flavors.
        /// Process one input sample. Output is stereo.
        /// </summary>
        /// <param name="din"></param>
        /// <returns></returns>
        public virtual Sample Next2(double din)
        {
            throw new Exception("You called virtual method Next2()");
        }

        ///// <summary>
        ///// Start a note with the given frequency and amplitude and channel number.
        ///// Implementers may interpret as KeyOn().
        ///// </summary>
        ///// <param name="channelNumber"></param>
        ///// <param name="noteNumber"></param>
        ///// <param name="amplitude"></param>
        //public virtual void NoteOn(int channelNumber, double noteNumber, double amplitude)
        //{
        //    // Non-implementers can ignore.
        //}

        ///// <summary>
        ///// Stop a note with the given amplitude (speed of decay) and channel number.
        ///// Implementers may interpret as KeyOn().
        ///// </summary>
        ///// <param name="channelNumber"></param>
        ///// <param name="noteNumber"></param>
        ///// <param name="amplitude"></param>
        //public virtual void NoteOff(int channelNumber, double noteNumber, double amplitude = 0.0)
        //{
        //    // Non-implementers can ignore.
        //}

        /// <summary>
        /// Start a note with the given frequency and amplitude.
        /// Implementers may interpret as KeyOn().
        /// </summary>
        /// <param name="noteNumber"></param>
        /// <param name="amplitude"></param>
        public virtual void NoteOn(double noteNumber, double amplitude)
        {
            // Non-implementers can ignore.
        }

        /// <summary>
        /// Stop a note with the given amplitude (speed of decay).
        /// Implementers may interpret as KeyOn().
        /// </summary>
        /// <param name="noteNumber"></param>
        /// <param name="amplitude"></param>
        public virtual void NoteOff(double noteNumber, double amplitude = 0.0)
        {
            // Non-implementers can ignore.
        }

        // Perform the control change specified by number and value.
        // public abstract void ControlChange(int controlId, object value);


        // implementation specific
        public virtual void Reset()
        {
            // Non-implementers can ignore.
        }
        #endregion

        #region Private functions

        #endregion
    }
}
