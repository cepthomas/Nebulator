using System;
using System.Collections.Generic;
using NBagOfTricks;
using Nebulator.Common;
using Nebulator.Device;


namespace Nebulator.OSC
{
    /// <summary>
    /// Abstraction layer between OSC comm and Nebulator steps. aka OSC client.
    /// </summary>
    public class OscOutput : NOutput
    {
        #region Fields
        /// <summary>OSC output device.</summary>
        NebOsc.Output _oscOutput;

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
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public OscOutput()
        {
        }

        /// <inheritdoc />
        public bool Init(string name)
        {
            bool inited = false;

            if (_oscOutput != null)
            {
                _oscOutput.Dispose();
                _oscOutput = null;
            }

            // Check for properly formed port.
            List<string> parts = name.SplitByToken(":");
            if (parts.Count == 3)
            {
                if (int.TryParse(parts[2], out int port))
                {
                    string ip = parts[1];
                    _oscOutput = new NebOsc.Output() { RemoteIP = ip, RemotePort = port };

                    if (_oscOutput.Init())
                    {
                        inited = true;
                        DeviceName = _oscOutput.DeviceName;
                        _oscOutput.LogEvent += OscOutput_LogEvent;
                    }
                    else
                    {
                        LogMsg(DeviceLogCategory.Error, $"Init OSC out failed");
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

                            if (non.Duration.TotalTocks > 0) // specific duration
                            {
                                // Remove any lingering note offs and add a fresh one.
                                _stops.RemoveAll(s => s.NoteNumber == non.NoteNumber && s.ChannelNumber == non.ChannelNumber);

                                _stops.Add(new StepNoteOff()
                                {
                                    Device = non.Device,
                                    ChannelNumber = non.ChannelNumber,
                                    NoteNumber = MathUtils.Constrain(non.NoteNumber, 0, OscCommon.MAX_NOTE),
                                    Expiry = non.Duration.TotalTocks
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
                            LogMsg(DeviceLogCategory.Send, step.ToString());
                        }
                        else
                        {
                            LogMsg(DeviceLogCategory.Error, step.ToString());
                        }
                    }
                    else
                    {
                        LogMsg(DeviceLogCategory.Error, step.ToString());
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
            DeviceLogEvent?.Invoke(this, new DeviceLogEventArgs() { DeviceLogCategory = OscCommon.TranslateLogCategory(e.LogCategory), Message = e.Message });
        }

        /// <summary>
        /// Ask host to do something with this.
        /// </summary>
        /// <param name="cat"></param>
        /// <param name="msg"></param>
        void LogMsg(DeviceLogCategory cat, string msg)
        {
            DeviceLogEvent?.Invoke(this, new DeviceLogEventArgs() { DeviceLogCategory = cat, Message = msg });
        }
        #endregion
    }
}
