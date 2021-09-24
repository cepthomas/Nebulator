using System;
using System.Collections.Generic;
using NLog;
using NBagOfTricks;
using Nebulator.Common;


namespace Nebulator.OSC
{
    /// <summary>
    /// Abstraction layer between OSC comm and Nebulator steps. aka OSC client.
    /// </summary>
    public class OscOutput : IOutputDevice
    {
        #region Fields
        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.GetLogger("OscOutput");

        /// <summary>OSC output device.</summary>
        NebOsc.Output _oscOutput;

        /// <summary>Access synchronizer.</summary>
        readonly object _lock = new object();

        /// <summary>Notes to stop later.</summary>
        List<StepNoteOff> _stops = new List<StepNoteOff>();

        /// <summary>Resource clean up.</summary>
        bool _disposed = false;
        #endregion

        #region Properties
        /// <inheritdoc />
        public string DeviceName { get; private set; } = Definitions.UNKNOWN_STRING;

        /// <inheritdoc />
        public DeviceType DeviceType => DeviceType.OscOut;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public OscOutput()
        {
        }

        /// <inheritdoc />
        public bool Init()
        {
            bool inited = false;

            if (_oscOutput != null)
            {
                _oscOutput.Dispose();
                _oscOutput = null;
            }

            // Check for properly formed url:port.
            List<string> parts = UserSettings.TheSettings.OscOutDevice.SplitByToken(":");
            if (parts.Count == 2)
            {
                if (int.TryParse(parts[1], out int port))
                {
                    string ip = parts[0];
                    _oscOutput = new NebOsc.Output() { RemoteIP = ip, RemotePort = port };

                    if (_oscOutput.Init())
                    {
                        inited = true;
                        DeviceName = _oscOutput.DeviceName;
                        _oscOutput.LogEvent += OscOutput_LogEvent;
                    }
                    else
                    {
                        _logger.Error($"Init OSC out failed");
                        inited = false;
                    }
                }
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
                _oscOutput?.Dispose();
                _oscOutput = null;

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
                if (_oscOutput != null)
                {
                    List<int> msgs = new List<int>();
                    NebOsc.Message msg = null;

                    switch (step)
                    {
                        case StepNoteOn non:
                            // /noteon/ channel notenum vel
                            msg = new NebOsc.Message() { Address = "/noteon" };
                            msg.Data.Add(non.ChannelNumber);
                            msg.Data.Add(non.NoteNumber);
                            msg.Data.Add(non.VelocityToPlay);

                            if (non.Duration.TotalSubdivs > 0) // specific duration
                            {
                                // Remove any lingering note offs and add a fresh one.
                                _stops.RemoveAll(s => s.NoteNumber == non.NoteNumber && s.ChannelNumber == non.ChannelNumber);

                                _stops.Add(new StepNoteOff()
                                {
                                    Device = non.Device,
                                    ChannelNumber = non.ChannelNumber,
                                    NoteNumber = MathUtils.Constrain(non.NoteNumber, 0, OscCommon.MAX_NOTE),
                                    Expiry = non.Duration.TotalSubdivs
                                });
                            }
                            break;

                        case StepNoteOff noff:
                            // /noteoff/ channel notenum
                            msg = new NebOsc.Message() { Address = "/noteoff" };
                            msg.Data.Add(noff.ChannelNumber);
                            msg.Data.Add(noff.NoteNumber);

                            break;

                        case StepControllerChange ctl:
                            // /controller/ channel ctlnum val
                            msg = new NebOsc.Message() { Address = "/controller" };
                            msg.Data.Add(ctl.ChannelNumber);
                            msg.Data.Add(ctl.ControllerId);
                            msg.Data.Add(ctl.Value);
                            break;

                        case StepPatch stt:
                            // ignore n/a
                            break;

                        default:
                            break;
                    }

                    if (msg != null)
                    {
                        if(_oscOutput.Send(msg))
                        {
                            if(UserSettings.TheSettings.MonitorOutput)
                            {
                                _logger.Trace($"{TraceCat.SND} OscOut:{step}");
                            }
                        }
                        else
                        {
                            _logger.Error($"Send failed");
                        }
                    }
                    else
                    {
                        _logger.Error($"Send failed");
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
        }

        /// <inheritdoc />
        public void Stop()
        {
        }
        #endregion

        #region Private functions
        /// <summary>
        /// OSC has something to say.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OscOutput_LogEvent(object sender, NebOsc.LogEventArgs e)
        {
            if (e.IsError)
            {
                _logger.Error(e.Message);
            }
            else
            {
                _logger.Info(e.Message);
            }
        }
        #endregion
    }
}
