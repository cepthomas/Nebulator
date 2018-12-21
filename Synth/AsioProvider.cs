using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Nebulator.Synth
{
    public class AsioProvider : ISampleProvider, IDisposable
    {
        bool _disposed = false;
        AsioOut _asioOut = null;

        public const int SAMPLE_RATE = 44100;

        public WaveFormat WaveFormat { get; }

        public AsioProvider()
        {
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(SAMPLE_RATE, 2);

        }

        public bool Init(string dev)
        {
            bool ok = true;

            _asioOut = new AsioOut(dev);

            _asioOut.Init(this);

            return ok;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            //TODOX test script.playing first

            // something like this?
            float amplitude = 0.25f;
            float frequency = 1000;
            for (int n = 0; n < count; n++)
            {
                float sample = (float)(amplitude * Math.Sin((2 * Math.PI * n * frequency) / AsioProvider.SAMPLE_RATE));
                //waveFileWriter.WriteSample(sample);
                buffer[n] = sample;
            }

            return count;
        }

        public void Play()
        {
            _asioOut?.Play(); // start playing - calls this.Read()
        }

        public void Stop()
        {
            _asioOut?.Stop();
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _asioOut?.Stop();
                _asioOut?.Dispose();
                _asioOut = null;

                _disposed = true;
            }
        }

        void testTODOX()
        {
            var anames = AsioOut.GetDriverNames();
            var asioOut = new AsioOut(anames[1]);
            var outputChannels = asioOut.DriverOutputChannelCount;
            asioOut.Init(this);
            asioOut.Play(); // start playing - calls this.Read()
            System.Threading.Thread.Sleep(2000);
            asioOut.Stop();
            asioOut.Dispose();
        }
    }
}
