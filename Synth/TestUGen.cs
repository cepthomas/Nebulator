using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;


namespace Nebulator.Synth
{
    class TestUGen : IUGen
    {
        //public WaveFormat WaveFormat => throw new NotImplementedException();
        public WaveFormat WaveFormat { get; } = new WaveFormat();

        public int NumInputs => throw new NotImplementedException();

        public int NumOutputs => throw new NotImplementedException();

        public TestUGen()
        {
 //TODOX           WaveFormat = WaveFormat.CreateIeeedoubleWaveFormat(44100, 2);
        }

        double _val = 0.0f;

        public int Read(float[] buffer, int offset, int count)
        {
            float amplitude = 0.25f;
            float frequency = 1000;
            for (int n = 0; n < count; n++)
            {
                float sample = (float)(amplitude * Math.Sin((2 * Math.PI * n * frequency) / WaveFormat.SampleRate));
                //waveFileWriter.WriteSample(sample);
                buffer[n] = sample;
            }


            ////// mono to stereo:
            // var sourceSamplesRequired = count / 2;
            // var outIndex = offset;
            // EnsureSourceBuffer(sourceSamplesRequired);
            // var sourceSamplesRead = source.Read(sourceBuffer, 0, sourceSamplesRequired);
            // for (var n = 0; n < sourceSamplesRead; n++)
            // {
            //     buffer[outIndex++] = sourceBuffer[n] * LeftVolume;
            //     buffer[outIndex++] = sourceBuffer[n] * RightVolume;
            // }
            // return sourceSamplesRead * 2;


            //for (int i = 0; i < count; i++)
            //{
            //    buffer[i] = _val;
            //}

            //_val += 0.1f;
            //if(_val >= 0.5f)
            //{
            //    _val = -0.5f;
            //}

            return count;
        }

        public void NoteOn(double frequency, double amplitude)
        {
            throw new NotImplementedException();
        }

        public void NoteOff(double amplitude)
        {
            throw new NotImplementedException();
        }

        public void ControlChange(string controlId, object value)
        {
            throw new NotImplementedException();
        }

        public bool Create(string utype, int id)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        void test()
        {
            var anames = AsioOut.GetDriverNames();

            var asioOut = new AsioOut(anames[1]);

            var outputChannels = asioOut.DriverOutputChannelCount;

           // MySamples smpl = new MySamples();

            asioOut.Init(this);

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


            asioOut.Play(); // start playing

            System.Threading.Thread.Sleep(2000);

            asioOut.Stop();

            asioOut.Dispose();
        }

    }

}
