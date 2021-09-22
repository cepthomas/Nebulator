using System;
using System.Collections.Generic;
using NLog;
using NBagOfTricks;
using Nebulator.Common;
using Nebulator.Steps;


namespace Nebulator.OSC
{
    /// <summary>
    /// Abstraction layer between OSC comm and Nebulator steps. aka OSC server.
    /// </summary>
    public class OscInput : IInputDevice
    {
        #region Fields
        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>OSC input device.</summary>
        NebOsc.Input _oscInput = null;

        /// <summary>Resource clean up.</summary>
        bool _disposed = false;
        #endregion

        #region Events
        /// <inheritdoc />
        public event EventHandler<DeviceInputEventArgs> DeviceInputEvent;
        #endregion

        #region Properties
        /// <inheritdoc />
        public string DeviceName { get; private set; } = Definitions.UNKNOWN_STRING;

        /// <inheritdoc />
        public DeviceType DeviceType => DeviceType.OscIn;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public OscInput()
        {
        }

        /// <inheritdoc />
        public bool Init()
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
                if (int.TryParse(UserSettings.TheSettings.OscInDevice, out int port))
                {
                    _oscInput = new NebOsc.Input() { LocalPort = port };
                    inited = true;
                    DeviceName = _oscInput.DeviceName;
                    _oscInput.InputEvent += OscInput_InputEvent;
                    _oscInput.LogEvent += OscInput_LogEvent;
                }
            }
            catch (Exception ex)
            {
                inited = false;
                _logger.Error($"Init OSC in failed: {ex.Message}");
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
            switch (e.LogCategory)
            {
                case NebOsc.LogCategory.Info: _logger.Info(e.Message); break;
                case NebOsc.LogCategory.Send: _logger.Trace($"SEND:{e.Message}"); break;
                case NebOsc.LogCategory.Recv: _logger.Trace($"RECV:{e.Message}"); break;
                case NebOsc.LogCategory.Error: _logger.Error(e.Message); break;
            }
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
                        _logger.Error($"Invalid address: {m.Address}");
                        break;
                }

                if (step != null)
                {
                    // Pass it up for handling.
                    DeviceInputEventArgs args = new DeviceInputEventArgs() { Step = step };
                    DeviceInputEvent?.Invoke(this, args);
                    _logger.Trace($"RECV:{step}");
                }
            });
        }
        #endregion
    }
}
