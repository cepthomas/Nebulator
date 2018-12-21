using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;



namespace Nebulator.Synth
{
    // SinOsc osc;
    // ADSR adsr;
    // Filter flt;
    // Pan pan;

    public class SinOsc : UGen
    {
        public SinOsc() : base()
        {
        }

        public override int Read(float[] buffer, int offset, int count)
        {
            //// The following example shows one second of a 1kHz sine wave being written to a WAV
            //// file using the WriteSample function:
            //double amplitude = 0.25f;
            //double frequency = 1000;
            //double samplemax = 0.0f;
            //double samplemin = 0.0f;
            //for (int n = 0; n < smpl.WaveFormat.SampleRate; n++)
            //{
            //    double sample = (double)(amplitude * Math.Sin((2 * Math.PI * n * frequency) / smpl.WaveFormat.SampleRate));
            //    //waveFileWriter.WriteSample(sample);
            //    samplemax = Math.Max(samplemax, sample);
            //    samplemin = Math.Min(samplemin, sample);
            //}

            return count;
        }

        public override void ControlChange(int controlId, object value)
        {

        }

        /// Start a note with the given frequency and amplitude.
        public override void NoteOn(double frequency, double amplitude)
        {

        }

        /// Stop a note with the given amplitude (speed of decay).
        public override void NoteOff(double amplitude = 0.0)
        {

        }

        ///
        public override void Clear()
        {

        }

        /// and/or
        public override void Reset()
        {

        }
    }
}
