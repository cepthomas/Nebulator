using System;
using System.Collections.Generic;
using NBagOfTricks.Utils;
using Nebulator.Common;
using Nebulator.Device;


namespace Nebulator.OSC
{
    /// <summary>
    /// Abstraction layer between OSC comm and Nebulator steps. aka OSC server.
    /// </summary>
    public class OscInput : NInput
    {
        #region Fields
        /// <summary>OSC input device.</summary>
        NebOsc.Input _oscInput = null;

        /// <summary>Resource clean up.</summary>
        bool _disposed = false;
        #endregion

        #region Events
        /// <inheritdoc />
        public event EventHandler<DeviceInputEventArgs> DeviceInputEvent;

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
        public OscInput()
        {
        }

        /// <inheritdoc />
        public bool Init(string name)
        {
            bool inited = false;

            DeviceName = "Invalid"; // default

            try
            {
                if (_oscInput != null)
                {
                    _oscInput.Dispose();
                    _oscInput = null;
                }

                // Check for properly formed port.
                List<string> parts = name.SplitByToken(":");
                if (parts.Count == 2)
                {
                    if (int.TryParse(parts[1], out int port))
                    {
                        _oscInput = new NebOsc.Input() { LocalPort = port };
                        inited = true;
                        DeviceName = _oscInput.DeviceName;
                        _oscInput.InputEvent += OscInput_InputEvent;
                        _oscInput.LogEvent += OscInput_LogEvent;
                    }
                }
            }
            catch (Exception ex)
            {
                inited = false;
                LogMsg(DeviceLogCategory.Error, $"Init OSC in failed: {ex.Message}");
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
                _oscInput?.Dispose();
                _oscInput = null;

                _disposed = true;
            }
        }
        #endregion

        #region Public functions
        /// <inheritdoc />
        public void Start()
        {
        }

        /// <inheritdoc />
        public void Stop()
        {
        }

        /// <inheritdoc />
        public void Housekeep()
        {
        }
        #endregion

        #region Private functions
        /// <summary>
        /// OSC has something to say.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OscInput_LogEvent(object sender, NebOsc.LogEventArgs e)
        {
            DeviceLogEvent?.Invoke(this, new DeviceLogEventArgs() { DeviceLogCategory = OscCommon.TranslateLogCategory(e.LogCategory), Message = e.Message });
        }

        /// <summary>
        /// Handle a received message.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OscInput_InputEvent(object sender, NebOsc.InputEventArgs e)
        {
            // message could be:
            // /note/ channel notenum vel
            // /controller/ channel ctlnum val

            e.Messages.ForEach(m =>
            {
                Step step = null;

                switch (m.Address)
                {
                    case "/note/":
                        if (m.Data.Count == 3)
                        {
                            int channel = MathUtils.Constrain((int)m.Data[0], 0, 100);
                            double notenum = MathUtils.Constrain((int)m.Data[1], 0, OscCommon.MAX_NOTE);
                            double velocity = MathUtils.Constrain((int)m.Data[2], 0, 1.0);

                            if (velocity == 0)
                            {
                                step = new StepNoteOff()
                                {
                                    Device = this,
                                    ChannelNumber = channel,
                                    NoteNumber = notenum,
                                    Velocity = 0
                                };
                            }
                            else
                            {
                                step = new StepNoteOn()
                                {
                                    Device = this,
                                    ChannelNumber = channel,
                                    NoteNumber = notenum,
                                    Velocity = velocity,
                                    VelocityToPlay = velocity,
                                    Duration = new Time(0)
                                };
                            }
                        }
                        break;

                    case "/controller/":
                        if (m.Data.Count == 3)
                        {
                            int channel = MathUtils.Constrain((int)m.Data[0], 0, 100);
                            int ctlnum = (int)m.Data[1];
                            double value = MathUtils.Constrain((int)m.Data[2], 0, 10000);

                            step = new StepControllerChange()
                            {
                                Device = this,
                                ChannelNumber = channel,
                                ControllerId = ctlnum,
                                Value = value
                            };
                        }
                        break;

                    default:
                        LogMsg(DeviceLogCategory.Error, $"Invalid address: {m.Address}");
                        break;
                }

                if (step != null)
                {
                    // Pass it up for handling.
                    DeviceInputEventArgs args = new DeviceInputEventArgs() { Step = step };
                    DeviceInputEvent?.Invoke(this, args);
                    LogMsg(DeviceLogCategory.Recv, step.ToString());
                }

            });
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
