using System;
using System.Collections.Generic;
using System.Linq;
// using System.Text;
// using System.Text.RegularExpressions;
// using System.Threading.Tasks;
// using System.IO;
// using System.Net.Sockets;
// using System.Net;
// using System.Diagnostics;
// using System.Threading;
// using System.Drawing;
using NAudio.Wave;
using Nebulator.Common;
using Nebulator.Device;


namespace Nebulator.Synth
{
    /// <summary>
    /// Abstraction layer between Synth and Nebulator steps.
    /// </summary>
    public class SynthOutput : NOutput, ISampleProvider
    {
        #region Fields
        /// <summary>ASIO device.</summary>
        AsioOut _asioOut = null;

        /// <summary>Access synchronizer.</summary>
        object _lock = new object();

        /// <summary>Notes to stop later.</summary>
        List<StepNoteOff> _stops = new List<StepNoteOff>();

        /// <summary>Resource clean up.</summary>
        bool _disposed = false;
        #endregion

        #region Events
        /// <inheritdoc />
        public event EventHandler<DeviceLogEventArgs> DeviceLogEvent;
        #endregion

        #region Properties
        /// <inheritdoc />
        public string DeviceName { get; private set; } = Utils.UNKNOWN_STRING;

        /// <summary>Main device to execute.</summary>
        public UGen SynthUGen { get; set; } = null;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public SynthOutput()
        {
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(SynthCommon.SAMPLE_RATE, SynthCommon.NUM_CHANNELS);
        }

        /// <inheritdoc />
        public bool Init(string name)
        {
            bool inited = false;

            DeviceName = "Invalid"; // default

            try
            {
                if (_asioOut != null)
                {
                    _asioOut.Dispose();
                    _asioOut = null;
                }

                List<string> parts = name.SplitByToken(":");
                if (parts.Count == 2)
                {
                    // Figure out which device.
                    int ind = AsioOut.GetDriverNames().ToList().IndexOf(parts[1]);

                    if (ind < 0)
                    {
                        LogMsg(DeviceLogEventArgs.LogCategory.Error, $"Invalid asio: {name}");
                    }
                    else
                    {
                        _asioOut = new AsioOut(parts[1]);
                        _asioOut.Init(this);
                        _asioOut.PlaybackStopped += AsioOut_PlaybackStopped;
                        inited = true;
                        DeviceName = parts[1];
                    }
                }
            }
            catch (Exception ex)
            {
                LogMsg(DeviceLogEventArgs.LogCategory.Error, $"Init synth out failed: {ex.Message}");
                inited = false;
            }

            return inited;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AsioOut_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            // TODOX all this:
            // Manually Stopping Playback
            // You can stop audio playback any time by simply calling Stop. Depending on the implementation of IWavePlayer, playback may not stop instantaneously, but finish playing the currently queued buffer (usually no more than 100ms). So even when you call Stop, you should wait for the PlaybackStopped event to be sure that playback has actually stopped.

            // Reaching the end of the input audio
            // In NAudio, the Read method on IWaveProvider is called every time the output device needs more audio to play. The Read method should normally return the requested number of bytes of audio (the count parameter). If Read returns less than count this means this is the last piece of audio in the input stream. If Read returns 0, the end has been reached.

            // NAudio playback devices will stop playing when the IWaveProvider's Read method returns 0. This will cause the PlaybackStopped event to get raised.

            // Output device error
            // If there is any kind of audio error during playback, the PlaybackStopped event will be fired, and the Exception property set to whatever exception caused playback to stop. A very common cause of this would be playing to a USB device that has been removed during playback.

            // Disposing resources
            // Often when playback ends, you want to clean up some resources, such as disposing the output device, and closing any input files such as AudioFileReader. It is strongly recommended that you do this when you receive the PlaybackStopped event and not immediately after calling Stop. This is because in many IWavePlayer implementations, the audio playback code is on another thread, and you may be disposing resources that will still be used.

            // Note that NAudio attempts to fire the PlaybackStopped event on the SynchronizationContext the device was created on. This means in a WinForms or WPF application it is safe to access the GUI in the handler.
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
        #endregion

        #region Public functions
        /// <inheritdoc />
        public void Housekeep()
        {
            // Send any stops due.
            _stops.ForEach(s => { s.Expiry--; if (s.Expiry < 0) Send(s); });

            // Reset.
            _stops.RemoveAll(s => s.Expiry < 0);
        }

        /// <inheritdoc />
        public bool Send(Step step)
        {
            bool ret = true;

            // Critical code section.
            lock (_lock)
            {
                // if (_udpClient != null)
                // {
                //     List<int> msgs = new List<int>();
                //     Message msg = null;

                //     switch (step)
                //     {
                //         case StepNoteOn non:
                //             // /note/ channel notenum vel
                //             msg = new Message() { Address = "/note" };
                //             msg.Data.Add(non.ChannelNumber);
                //             msg.Data.Add(non.NoteNumber);
                //             msg.Data.Add(non.VelocityToPlay);

                //             if (non.Duration.TotalTocks > 0) // specific duration
                //             {
                //                 // Remove any lingering note offs and add a fresh one.
                //                 _stops.RemoveAll(s => s.NoteNumber == non.NoteNumber && s.ChannelNumber == non.ChannelNumber);

                //                 _stops.Add(new StepNoteOff()
                //                 {
                //                     Device = non.Device,
                //                     ChannelNumber = non.ChannelNumber,
                //                     NoteNumber = Utils.Constrain(non.NoteNumber, 0, OscCommon.MAX_NOTE),
                //                     Expiry = non.Duration.TotalTocks
                //                 });
                //             }
                //             break;

                //         case StepNoteOff noff:
                //             // /note/ channel notenum 0
                //             msg = new Message() { Address = "/note" };
                //             msg.Data.Add(noff.ChannelNumber);
                //             msg.Data.Add(noff.NoteNumber);
                //             msg.Data.Add(0);

                //             break;

                //         case StepControllerChange ctl:
                //             // /controller/ channel ctlnum val
                //             msg = new Message() { Address = "/controller" };
                //             msg.Data.Add(ctl.ChannelNumber);
                //             msg.Data.Add(ctl.ControllerId);
                //             msg.Data.Add(ctl.Value);
                //             break;

                //         case StepPatch stt:
                //             // ignore n/a
                //             break;

                //         default:
                //             break;
                //     }

                //     if (msg != null)
                //     {
                //         List<byte> bytes = msg.Pack();
                //         if (bytes != null)
                //         {
                //             if (msg.Errors.Count == 0)
                //             {
                //                 _udpClient.Send(bytes.ToArray(), bytes.Count);
                //                 LogMsg(DeviceLogEventArgs.LogCategory.Send, step.ToString());
                //             }
                //             else
                //             {
                //                 msg.Errors.ForEach(e => LogMsg(DeviceLogEventArgs.LogCategory.Error, e));
                //             }
                //         }
                //         else
                //         {
                //             LogMsg(DeviceLogEventArgs.LogCategory.Error, step.ToString());
                //         }
                //     }
                //     else
                //     {
                //         LogMsg(DeviceLogEventArgs.LogCategory.Error, step.ToString());
                //     }
                //}
            }

            return ret;
        }

        /// <inheritdoc />
        public void Kill(int? channel)
        {
        }

        /// <inheritdoc />
        public void Start()
        {
            _asioOut?.Play();
           // _asioOut?.Stop();
        }

        /// <inheritdoc />
        public void Stop()
        {
            _asioOut?.Pause();
        }
        #endregion

        #region ISampleProvider implementation
        public WaveFormat WaveFormat { get; }

        // This gets called when output is needed.
        public int Read(float[] buffer, int offset, int count)
        {
            if(SynthUGen != null)
            {
                for (int n = 0; n < count; n++)
                {
                    buffer[n] = (float)SynthUGen.Sample();//TODOX stereo?
                }
            }
            else
            {
                count = 0;//?
            }

            return count;
        }
        #endregion

        // stereo?
        // public int Read(float[] buffer, int offset, int sampleCount)
        // {
        //     int samplesRead = source.Read(buffer, offset, sampleCount);
        //     if (volume != 1f)
        //     {
        //         for (int n = 0; n < sampleCount; n++)
        //         {
        //             buffer[offset + n] *= volume;
        //         }
        //     }
        //     return samplesRead;
        // }


        // public int Read(float[] buffer, int offset, int count)
        // {
        //     var sourceSamplesRequired = count / 2;
        //     var outIndex = offset;
        //     EnsureSourceBuffer(sourceSamplesRequired);
        //     var sourceSamplesRead = source.Read(sourceBuffer, 0, sourceSamplesRequired);
        //     for (var n = 0; n < sourceSamplesRead; n++)
        //     {
        //         buffer[outIndex++] = sourceBuffer[n] * LeftVolume;
        //         buffer[outIndex++] = sourceBuffer[n] * RightVolume;
        //     }
        //     return sourceSamplesRead * 2;
        // }


        #region Private functions
        /// <summary>Ask host to do something with this.</summary>
        /// <param name="cat"></param>
        /// <param name="msg"></param>
        void LogMsg(DeviceLogEventArgs.LogCategory cat, string msg)
        {
            DeviceLogEvent?.Invoke(this, new DeviceLogEventArgs() { Category = cat, Message = msg });
        }
        #endregion
    }
}
