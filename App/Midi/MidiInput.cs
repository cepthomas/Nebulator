using System;
using System.Collections.Generic;
using NAudio.Midi;
using NLog;
using NBagOfTricks;
using Nebulator.Common;


namespace Nebulator.Midi
{
    /// <summary>
    /// Abstraction layer between NAudio midi and Nebulator steps.
    /// </summary>
    public class MidiInput : IInputDevice
    {
        #region Fields
        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.GetLogger("MidiInput");

        /// <summary>Midi input device.</summary>
        MidiIn? _midiIn;
        #endregion

        #region Events
        /// <inheritdoc />
        public event EventHandler<DeviceInputEventArgs>? DeviceInputEvent;
        #endregion

        #region Properties
        /// <inheritdoc />
        public string DeviceName { get; private set; } = Definitions.UNKNOWN_STRING;

        /// <inheritdoc />
        public DeviceType DeviceType => DeviceType.MidiIn;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MidiInput()
        {
        }

        /// <inheritdoc />
        public bool Init()
        {
            bool inited = false;

            DeviceName = "Invalid"; // default

            try
            {
                _midiIn?.Stop();
                _midiIn?.Dispose();
                _midiIn = null;

                // Figure out which device.
                List<string> devices = new();
                for (int i = 0; i < MidiIn.NumberOfDevices; i++)
                {
                    devices.Add(MidiIn.DeviceInfo(i).ProductName);
                }

                int ind = devices.IndexOf(UserSettings.TheSettings.MidiIn);

                if (ind < 0)
                {
                    _logger.Error($"Invalid midi input device:{UserSettings.TheSettings.MidiIn}");
                }
                else
                {
                    _midiIn = new MidiIn(ind);
                    _midiIn.MessageReceived += MidiIn_MessageReceived;
                    _midiIn.ErrorReceived += MidiIn_ErrorReceived;
                    _midiIn.Start();
                    inited = true;
                    DeviceName = UserSettings.TheSettings.MidiIn;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Init midi in failed: {ex.Message}");
                inited = false;
            }

            return inited;
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        public void Dispose()
        {
            _midiIn?.Stop();
            _midiIn?.Dispose();
            _midiIn = null;
        }
        #endregion

        #region Public functions
        /// <inheritdoc />
        public void Start()
        {
            _midiIn?.Start();
        }

        /// <inheritdoc />
        public void Stop()
        {
            _midiIn?.Stop();
        }

        /// <inheritdoc />
        public void Housekeep()
        {
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Process input midi event. Note that NoteOn with 0 velocity are converted to NoteOff.
        /// </summary>
        void MidiIn_MessageReceived(object? sender, MidiInMessageEventArgs e)
        {
            // Decode the message. We only care about a few.
            MidiEvent me = MidiEvent.FromRawMessage(e.RawMessage);
            Step? step = null;

            switch (me)
            {
                case NoteOnEvent evt:
                    {
                        if(evt.Velocity == 0)
                        {
                            step = new StepNoteOff()
                            {
                                Device = this,
                                ChannelNumber = evt.Channel,
                                NoteNumber = MathUtils.Constrain(evt.NoteNumber, 0, Definitions.MAX_MIDI),
                                Velocity = 0.0
                            };
                        }
                        else
                        {
                            step = new StepNoteOn()
                            {
                                Device = this,
                                ChannelNumber = evt.Channel,
                                NoteNumber = evt.NoteNumber,
                                Velocity = evt.Velocity / Definitions.MAX_MIDI,
                                VelocityToPlay = evt.Velocity / Definitions.MAX_MIDI,
                                Duration = new Time(0)
                            };
                        }
                    }
                    break;

                case NoteEvent evt:
                    {
                        step = new StepNoteOff()
                        {
                            Device = this,
                            ChannelNumber = evt.Channel,
                            NoteNumber = MathUtils.Constrain(evt.NoteNumber, 0, Definitions.MAX_MIDI),
                            Velocity = evt.Velocity / Definitions.MAX_MIDI
                        };
                    }
                    break;

                case ControlChangeEvent evt:
                    {
                        step = new StepControllerChange()
                        {
                            Device = this,
                            ChannelNumber = evt.Channel,
                            ControllerId = (ControllerDef)evt.Controller,
                            Value = evt.ControllerValue
                        };
                    }
                    break;

                case PitchWheelChangeEvent evt:
                    {
                        step = new StepControllerChange()
                        {
                            Device = this,
                            ChannelNumber = evt.Channel,
                            ControllerId = ControllerDef.PitchControl,
                            Value = MathUtils.Constrain(evt.Pitch, 0, MidiUtils.MAX_PITCH),
                        };
                    }
                    break;
            }

            if (step is not null)
            {
                // Pass it up for handling.
                DeviceInputEventArgs args = new() { Step = step };
                DeviceInputEvent?.Invoke(this, args);
                if(UserSettings.TheSettings.MonitorInput)
                {
                    _logger.Trace($"{TraceCat.RCV} MidiIn:{step}");
                }
            }
        }

        /// <summary>
        /// Process error midi event - Parameter 1 is invalid.
        /// </summary>
        void MidiIn_ErrorReceived(object? sender, MidiInMessageEventArgs e)
        {
            _logger.Error($"Message:0x{e.RawMessage:X8}");
        }
        #endregion
    }
}
