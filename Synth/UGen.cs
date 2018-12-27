using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;


namespace Nebulator.Synth
{
    /// <summary>Category types.</summary>
//    public enum UGenType { Generator, Processor }

    public abstract class UGen // TODOX stereo out, maybe in
    {
        // Chuck: All ugens have at least the following four parameters:
        // .gain - (float, READ/WRITE) - set gain.
        // .op - (int, READ/WRITE) - set operation type  
        //    0: stop - always output 0.
        //    1: normal operation, add all inputs (default).
        //    2: normal operation, subtract all inputs starting from the earliest connected.
        //    3: normal operation, multiply all inputs.    
        //    4 : normal operation, divide inputs starting from the earlist connected.
        //    -1: passthru - all inputs to the ugen are summed and passed directly to output.  
        // .last - (float, READ/WRITE) - returns the last sample computed by the unit generator as a float.
        // .channels - (int, READ only) - the number channels on the UGen
        // .chan - (int) - returns a reference on a channel (0 -N-1)
        // .isConnectedTo( Ugen )  returns 1 if the unit generator connects to the argument, 0 otherwise.
        //
        // ------  Multichannel UGens are adc, dac, Pan2, Mix2


        //"Set the ugen's operation mode. Accepted values are: 1 (sum inputs), 2 (take difference between first input and subsequent inputs), 3 (multiply inputs), 4 (divide first input by subsequent inputs), 0 (do not synthesize audio, output 0) or -1 (passthrough inputs to output).";


        // class UGenInput
        // {
        //     public UGen Input { get; set; } = null;
        //     public double Gain { get; set; } = 0.2;
        //     // public double Pan1 { get; set; } = 0.0;
        // }
        // List<UGenInput> _inputs = new List<UGenInput>();
        // public void AddInput(UGen ugen)
        // {
        //     _inputs.Add(new UGenInput() { Input = ugen });
        // }


        // public UGenType UGenType { get; } = 0;

        // <summary>Zero or more inputs are summed.</summary>
        //public List<UGen> Inputs { get; } = new List<UGen>();
        //public UGen Input { get; set; } = null;
        //public UGen Input1 { get; set;  } = null; // Left or mono
        //public UGen Input2 { get; set; } = null; // Right






        #region Fields

        #endregion




        #region Properties
        // /// <summary></summary>
        // public int NumInputs { get; } = 0;

        /// <summary></summary>
        public int NumOutputs { get; } = 0;

        // 0.0 to 1.0
        //public double Gain { get; set; } = 0.5;
        public double Gain1 { get; set; } = 0.5; // Left or mono
        public double Gain2 { get; set; } = 0.5; // Right

        #endregion

        #region Lifecycle
        protected UGen()
        {
        }
        #endregion


        #region Public Functions - virtual
        // Process one sample
        public virtual double Sample(double din = 0)
        {
            throw new Exception("Virtual method");
            //return din * Gain1;
        }

        // Process one sample with multiple inputs - mixer
        public virtual double Sample(double din1, double din2)
        {
            throw new Exception("Virtual method");
            //return (din1 + din2) * Gain1;
        }

        // a buffer of samples - was float
        //        public abstract int Sample(double[] buffer, int offset, int count);

        // Perform the control change specified by number and value.
        //        public abstract void ControlChange(int controlId, object value);

        /// Start a note with the given frequency and amplitude.
        public virtual void NoteOn(double noteNumber, double amplitude)
        {
            throw new Exception("Virtual method");
        }


        /// Stop a note with the given amplitude (speed of decay).
        public virtual void NoteOff(double noteNumber, double amplitude = 0.0)
        {
            throw new Exception("Virtual method");
        }


        // Start envelope toward "on" target.
        //void keyOn();
        /// Start envelope toward "off" target.
        //void keyOff();



        // implementation specific
        public virtual void Reset()
        {
            throw new Exception("Virtual method");
        }
        #endregion

        #region Private functions

        #endregion
    }
}
