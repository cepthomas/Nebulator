using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;
using Nebulator.Common;
using Nebulator.Device;


// TODON2 On the performance front, the killer is the .NET garbage collector. You have to hope that it
//doesn't kick in during a callback. With ASIO you often run at super low latencies (<10ms), and
//the garbage collector can cause a callback to be missed, meaning you'd hear a glitch in the audio output.
// NOW (4.5) >>> While the SustainedLowLatency setting is in effect, generation 0, generation 1, and background generation 2 
//collections still occur and do not typically cause noticeable pause times. A blocking generation 2 collection happens
//only if the machine is low in memory or if the app induces a GC by calling GC.Collect(). It is critical that you deploy
//apps that use the SustainedLowLatency setting onto machines that have adequate memory, so they will satisfy the resulting
//growth in the heap while the setting is in effect.

// TODON2 FBP? http://www.jpaulmorrison.com/fbp/  https://github.com/jpaulm
//This approach to organizing and implementing signal flow through a synthesizer was very much inspired by J. Paul Morrison's website on flow-based programming. The idea is that you have a collection of components connected in some way with data flowing through them. It is easy to change and rearrange components to create new configurations. I'm a strong believer in this approach.

// AsioOut also has:
// public void ShowControlPanel()
// public int PlaybackLatency;
// public PlaybackState;
// public string DriverName;
// public int NumberOfOutputChannels;}
// public int NumberOfInputChannels;
// public int DriverInputChannelCount;
// public int DriverOutputChannelCount;
// public int FramesPerBuffer;


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
        readonly object _lock = new object();

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
        public UGen2 Synth { get; set; } = null;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public SynthOutput()
        {
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(SynthCommon.SampleRate, SynthCommon.NumOutputs);
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
                        LogMsg(DeviceLogCategory.Error, $"Invalid asio: {name}");
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
                LogMsg(DeviceLogCategory.Error, $"Init synth out failed: {ex.Message}");
                inited = false;
            }

            return inited;
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
        public bool Send(Step step) // TODON1 Synth doesn't really fit in the current model. It's not an external device.
        {
            bool ret = true;

            // Critical code section.
            lock (_lock)
            {
                if (_asioOut != null && Synth != null)
                {
                    switch (step)
                    {
                        case StepNoteOn non:
                            Synth.Note(non.NoteNumber, non.VelocityToPlay);
 
                            if (non.Duration.TotalTocks > 0) // specific duration
                            {
                                // Remove any lingering note offs and add a fresh one.
                                _stops.RemoveAll(s => s.NoteNumber == non.NoteNumber && s.ChannelNumber == non.ChannelNumber);
 
                                _stops.Add(new StepNoteOff()
                                {
                                    Device = non.Device,
                                    ChannelNumber = non.ChannelNumber,
                                    NoteNumber = Utils.Constrain(non.NoteNumber, 0, 127 /*SynthCommon.MAX_NOTE*/),
                                    Expiry = non.Duration.TotalTocks
                                });
                            }
                            break;
 
                        case StepNoteOff noff:
                            Synth.Note(noff.NoteNumber, 0);
                            break;
 
                        case StepControllerChange ctl:
                        case StepPatch stt:
                        // ignore n/a
                        default:
                            break;
                    }
                }
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
        }

        /// <inheritdoc />
        public void Stop()
        {
            _asioOut?.Pause(); // ? or Stop();
        }
        #endregion

        #region ISampleProvider implementation
        /// <summary></summary>
        public WaveFormat WaveFormat { get; }

        /// <summary>
        /// This ISampleProvider function gets called when output is needed.
        /// </summary>
        /// <param name="buffer">Stereo with interleaved L/R.</param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public int Read(float[] buffer, int offset, int count)
        {
            if(Synth != null)
            {
                for (int n = 0; n < count;)
                {
                    Sample dout = Synth.Next(0);
                    buffer[n++] = (float)dout.Left;
                    buffer[n++] = (float)dout.Right;
                }

                //for (int n = 0; n < count;)
                //{
                //    _dummy[n] = buffer[n];
                //}
            }
            else
            {
                count = 0; // this should stop the device
            }

            return count;
        }
        float[] _dummy = new float[1024]; // TODON2 this could stress-test GC too
        #endregion

        #region Private functions
        /// <summary>
        /// Asio callback handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AsioOut_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            // Manually Stopping Playback
            // You can stop audio playback any time by simply calling Stop. Depending on the implementation of IWavePlayer,
            // playback may not stop instantaneously, but finish playing the currently queued buffer (usually no more
            // than 100ms). So even when you call Stop, you should wait for the PlaybackStopped event to be sure that
            // playback has actually stopped.

            // Reaching the end of the input audio
            // In NAudio, the Read method on IWaveProvider is called every time the output device needs more audio to play.
            // The Read method should normally return the requested number of bytes of audio (the count parameter). If
            // Read returns less than count this means this is the last piece of audio in the input stream. If Read
            // returns 0, the end has been reached.

            // NAudio playback devices will stop playing when the IWaveProvider's Read method returns 0. This will cause
            // the PlaybackStopped event to get raised.

            // Output device error
            // If there is any kind of audio error during playback, the PlaybackStopped event will be fired, and the
            // Exception property set to whatever exception caused playback to stop. A very common cause of this would
            // be playing to a USB device that has been removed during playback.

            // Disposing resources
            // Often when playback ends, you want to clean up some resources, such as disposing the output device, and
            // closing any input files such as AudioFileReader. It is strongly recommended that you do this when you
            // receive the PlaybackStopped event and not immediately after calling Stop. This is because in many
            // IWavePlayer implementations, the audio playback code is on another thread, and you may be disposing
            // resources that will still be used.

            // Note that NAudio attempts to fire the PlaybackStopped event on the SynchronizationContext the device was
            // created on. This means in a WinForms or WPF application it is safe to access the GUI in the handler.
        }

        /// <summary>Ask host to do something with this.</summary>
        /// <param name="cat"></param>
        /// <param name="msg"></param>
        void LogMsg(DeviceLogCategory cat, string msg)
        {
            DeviceLogEvent?.Invoke(this, new DeviceLogEventArgs() { DeviceLogCategory = cat, Message = msg });
        }
        #endregion
    }
}
