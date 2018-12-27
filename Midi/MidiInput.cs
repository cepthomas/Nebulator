using System;
using System.Collections.Generic;
using NAudio.Midi;
using Nebulator.Common;
using Nebulator.Device;


// TODO: Support microtonal notes with Pitch changes.
// Pitch Bend Range can be set by sending MIDI controller messages. Specifically, you do it with Registered Parameters (cc# 100 and 101).
// On the MIDI channel in question, you need to send:
// MIDI cc101  = 0
// MIDI cc100  = 0
// MIDI cc6    = value of desired bend range (in semitones)
// Example: Lets say you want to set the bend range to 2 semi-tones. First you send cc# 100 with a value of 0; then cc#101 with a value of 0. This turns on reception for setting pitch bend with the Data controller (#6). Then you send cc# 6 with a value of 2 (in semitones; this will give you a whole step up and a whole step down from the center).
// Once you have set the bend range the way you want, then you send controller 100 or 101 with a value of 127 so that any further messages of controller 6 (which you might be using for other stuff) won't change the bend range.

namespace Nebulator.Midi
{
    /// <summary>
    /// Abstraction layer between NAudio midi and Nebulator steps.
    /// </summary>
    public class MidiInput : NInput
    {
        #region Fields
        /// <summary>Midi input device.</summary>
        MidiIn _midiIn = null;

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
        public MidiInput()
        {
        }

        /// <inheritdoc />
        public bool Init(string name)
        {
            bool inited = false;

            DeviceName = "Invalid"; // default

            try
            {
                if (_midiIn != null)
                {
                    _midiIn.Stop();
                    _midiIn.Dispose();
                    _midiIn = null;
                }

                List<string> parts = name.SplitByToken(":");
                if(parts.Count == 2)
                {
                    // Figure out which device.
                    List<string> devices = new List<string>();
                    for (int device = 0; device < MidiIn.NumberOfDevices; device++)
                    {
                        devices.Add(MidiIn.DeviceInfo(device).ProductName);
                    }

                    int ind = devices.IndexOf(parts[1]);

                    if (ind < 0)
                    {
                        LogMsg(DeviceLogEventArgs.LogCategory.Error, $"Invalid midi: {parts[1]}");
                    }
                    else
                    {
                        _midiIn = new MidiIn(ind);
                        _midiIn.MessageReceived += MidiIn_MessageReceived;
                        _midiIn.ErrorReceived += MidiIn_ErrorReceived;
                        _midiIn.Start();
                        inited = true;
                        DeviceName = parts[1];
                    }
                }
            }
            catch (Exception ex)
            {
                LogMsg(DeviceLogEventArgs.LogCategory.Error, $"Init midi in failed: {ex.Message}");
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
                _midiIn?.Stop();
                _midiIn?.Dispose();
                _midiIn = null;

                _disposed = true;
            }
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
        void MidiIn_MessageReceived(object sender, MidiInMessageEventArgs e)
        {
            // Decode the message. We only care about a few.
            MidiEvent me = MidiEvent.FromRawMessage(e.RawMessage);
            Step step = null;

            switch (me.CommandCode)
            {
                case MidiCommandCode.NoteOn:
                    {
                        NoteOnEvent evt = me as NoteOnEvent;

                        if(evt.Velocity == 0)
                        {
                            step = new StepNoteOff()
                            {
                                Device = this,
                                ChannelNumber = evt.Channel,
                                NoteNumber = Utils.Constrain(evt.NoteNumber, 0, MidiUtils.MAX_MIDI),
                                Velocity = 0
                            };
                        }
                        else
                        {
                            step = new StepNoteOn()
                            {
                                Device = this,
                                ChannelNumber = evt.Channel,
                                NoteNumber = evt.NoteNumber,
                                Velocity = evt.Velocity,
                                VelocityToPlay = evt.Velocity,
                                Duration = new Time(0)
                            };
                        }
                    }
                    break;

                case MidiCommandCode.NoteOff:
                    {
                        NoteEvent evt = me as NoteEvent;
                        step = new StepNoteOff()
                        {
                            Device = this,
                            ChannelNumber = evt.Channel,
                            NoteNumber = Utils.Constrain(evt.NoteNumber, 0, MidiUtils.MAX_MIDI),
                            Velocity = evt.Velocity
                        };
                    }
                    break;

                case MidiCommandCode.ControlChange:
                    {
                        ControlChangeEvent evt = me as ControlChangeEvent;
                        step = new StepControllerChange()
                        {
                            Device = this,
                            ChannelNumber = evt.Channel,
                            ControllerId = (int)evt.Controller,
                            Value = evt.ControllerValue
                        };
                    }
                    break;

                case MidiCommandCode.PitchWheelChange:
                    {
                        PitchWheelChangeEvent evt = me as PitchWheelChangeEvent;
                        step = new StepControllerChange()
                        {
                            Device = this,
                            ChannelNumber = evt.Channel,
                            ControllerId = ScriptDefinitions.TheDefinitions.PitchControl,
                            Value = Utils.Constrain(evt.Pitch, 0, MidiUtils.MAX_PITCH),
                        };
                    }
                    break;
            }

            if (step != null)
            {
                // Pass it up for handling.
                DeviceInputEventArgs args = new DeviceInputEventArgs() { Step = step };
                DeviceInputEvent?.Invoke(this, args);
                LogMsg(DeviceLogEventArgs.LogCategory.Recv, step.ToString());
            }
        }

        /// <summary>
        /// Process error midi event - Parameter 1 is invalid.
        /// </summary>
        void MidiIn_ErrorReceived(object sender, MidiInMessageEventArgs e)
        {
            LogMsg(DeviceLogEventArgs.LogCategory.Error, $"Message:0x{e.RawMessage:X8}");
        }

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
