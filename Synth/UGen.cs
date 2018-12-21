using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;



// TODOX On the performance front, the killer is the .NET garbage collector. You have to hope that it
//doesn't kick in during a callback. With ASIO you often run at super low latencies (<10ms), and
//the garbage collector can cause a callback to be missed, meaning you'd hear a glitch in the audio output.

// see:
//public class SignalGenerator : ISampleProvider
//public class AdsrSampleProvider : ISampleProvider
//public class MultiplexingSampleProvider : ISampleProvider

//C:\Users\cet\Desktop\sound-xxx\minim-cpp-master\src\ugens

//C:\Users\cet\Desktop\sound-xxx/Minim-master/src/main/java/ddf/minim/ugens/Abs.java

//C:\Users\cet\Desktop\sound-xxx/processing-sound-master/src/processing/sound/SawOsc.java

//jsyn-master/src/com/jsyn/unitgen/SineOscillator.java



namespace Nebulator.Synth
{
    /// <summary>Category types.</summary>
    public enum UGenType { Generator, Processor }


    public abstract class UGen //: ISampleProvider
    {
        #region Properties
        /// <summary></summary>
        protected int NumInputs { get; }

        /// <summary></summary>
        protected int NumOutputs { get; }

        protected UGenType UGenType { get; }

        protected UGen Input { get; }

        #endregion

        protected UGen()
        {
            //WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(SAMPLE_RATE, 2);
            
        }

        // Chuck: All ugen's have at least the following four parameters:
        // 
        // .gain - (double, READ/WRITE) - set gain.
        // .op - (int, READ/WRITE) - set operation type  
        //    0: stop - always output 0.
        //    1: normal operation, add all inputs (default).
        //    2: normal operation, subtract all inputs starting from the earliest connected.
        //    3: normal operation, multiply all inputs.    
        //    4: normal operation, divide inputs starting from the earliest connected.
        //    -1: passthru - all inputs to the ugen are summed and passed directly to output.  
        // .last - (double, READ/WRITE) - returns the last sample computed by the unit generator as a double.
        // .channels - (int, READ only) - the number channels on the UGen
        // .chan - (int) - returns a reference on a channel (0 -N-1)
        // .isConnectedTo( Ugen )  returns 1 if the unit generator connects to the argument, 0 otherwise.


        #region Functions - must be supplied by implementations
        public abstract int Read(float[] buffer, int offset, int count);


        /// Perform the control change specified by number and value.
        public abstract void ControlChange(int controlId, object value);

        #endregion

        #region Functions - optional
        /// Start a note with the given frequency and amplitude.
        public virtual void NoteOn(double frequency, double amplitude)
        {

        }

        /// Start envelope toward "on" target.
        //void keyOn();

        /// Stop a note with the given amplitude (speed of decay).
        public virtual void NoteOff(double amplitude = 0.0)
        {

        }

        /// Start envelope toward "off" target.
        //void keyOff();


        /// Make a neb ugen from underlying library.
        //bool Create(string utype, int id);

        ///
        public virtual void Clear()
        {

        }

        /// and/or
        public virtual void Reset()
        {

        }

        #endregion

    }

    // interface IUGen : ISampleProvider
    // {
    //     UGenType UGenType { get; }
    //     IUGen Input { get; }
    //     /// <summary></summary>
    //     int NumInputs { get; }
    //     /// <summary></summary>
    //     int NumOutputs { get; }

    //     #region Functions
    //     /// Start a note with the given frequency and amplitude.
    //     void NoteOn(double frequency, double amplitude);
    //     /// Start envelope toward "on" target.
    //     //void keyOn();

    //     /// Stop a note with the given amplitude (speed of decay).
    //     void NoteOff(double amplitude = 0.0);
    //     /// Start envelope toward "off" target.
    //     //void keyOff();

    //     /// Perform the control change specified by id and value.
    //     void ControlChange(string controlId, object value);

    //     /// Perform the control change specified by number and value.
    //     //    void controlChange(int number, double value);

    //     /// Make a neb ugen from underlying library.
    //     bool Create(string utype, int id);


    //     ///
    //     void Clear();
    //     /// and/or
    //     void Reset();
    //     #endregion
    // }
}
