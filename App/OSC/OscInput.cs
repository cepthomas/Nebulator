using System;
using System.Collections.Generic;
using NBagOfTricks;
using NBagOfTricks.Slog;
using Nebulator.Common;


namespace Nebulator.OSC
{
    /// <summary>
    /// Abstraction layer between OSC comm and Nebulator steps. aka OSC server.
    /// </summary>
    public sealed class OscInput : IInputDevice
    {
        #region Fields
        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("OscInput");

        /// <summary>OSC input device.</summary>
        NebOsc.Input? _oscInput = null;
        #endregion

        #region Events
        /// <inheritdoc />
        public event EventHandler<DeviceInputEventArgs>? DeviceInputEvent;
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
                _oscInput?.Dispose();
                _oscInput = null;

                // Check for properly formed port.
                if (int.TryParse(UserSettings.TheSettings.OscIn, out int port))
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
            _oscInput?.Dispose();
            _oscInput = null;
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
        void OscInput_LogEvent(object? sender, NebOsc.LogEventArgs e)
        {
            if(e.IsError)
            {
                _logger.Error(e.Message);
            }
            else
            {
                _logger.Info(e.Message);
            }
        }

        /// <summary>
        /// Handle a received message.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OscInput_InputEvent(object? sender, NebOsc.InputEventArgs e)
        {
            // message could be:
            // /note/ channel notenum vel
            // /controller/ channel ctlnum val

            e.Messages.ForEach(m =>
            {
                Step? step = null;

                switch (m.Address)
                {
                    case "/note/":
                        if (m.Data.Count == 3)
                        {
                            int channel = MathUtils.Constrain((int)m.Data[0], 0, 100);
                            double notenum = MathUtils.Constrain((int)m.Data[1], 0, Definitions.MAX_MIDI);
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

                            if(Enum.IsDefined(typeof(ControllerDef), ctlnum))
                            {
                                step = new StepControllerChange()
                                {
                                    Device = this,
                                    ChannelNumber = channel,
                                    ControllerId = (ControllerDef)ctlnum,
                                    Value = value
                                };
                            }
                            else
                            {
                                _logger.Error($"Invalid controller: {ctlnum}");
                            }
                        }
                        break;

                    default:
                        _logger.Error($"Invalid address: {m.Address}");
                        break;
                }

                if (step is not null)
                {
                    // Pass it up for handling.
                    DeviceInputEventArgs args = new() { Step = step };
                    DeviceInputEvent?.Invoke(this, args);
                    if(UserSettings.TheSettings.MonitorInput)
                    {
                        _logger.Trace($"{TraceCat.RCV} OscIn:{step}");
                    }
                }
            });
        }
        #endregion
    }
}
