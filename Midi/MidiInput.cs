using System;
using System.Collections.Generic;
using NAudio.Midi;
using Nebulator.Common;
using Nebulator.Comm;


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
        public event EventHandler<CommInputEventArgs> CommInputEvent;

        /// <inheritdoc />
        public event EventHandler<CommLogEventArgs> CommLogEvent;
        #endregion

        #region Properties
        /// <inheritdoc />
        public string CommName { get; set; } = Utils.UNKNOWN_STRING;

        /// <inheritdoc />
        public bool Monitor { get; set; } = false;

        /// <inheritdoc />
        public CommCaps Caps { get; set; } = null;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MidiInput()
        {
            Caps = MidiUtils.GetCommCaps();
        }

        /// <inheritdoc />
        public bool Init()
        {
            bool ret = true;

            try
            {
                if (_midiIn != null)
                {
                    _midiIn.Stop();
                    _midiIn.Dispose();
                    _midiIn = null;
                }

                // Figure out which device.
                List<string> devices = new List<string>();
                for (int device = 0; device < MidiIn.NumberOfDevices; device++)
                {
                    devices.Add(MidiIn.DeviceInfo(device).ProductName);
                }

                int ind = devices.IndexOf(CommName);

                if(ind < 0)
                {
                    LogMsg(CommLogEventArgs.LogCategory.Error, $"Invalid midi: {CommName}");
                    ret = false;
                }
                else
                {
                    _midiIn = new MidiIn(ind);
                    _midiIn.MessageReceived += MidiIn_MessageReceived;
                    _midiIn.ErrorReceived += MidiIn_ErrorReceived;
                    _midiIn.Start();
                }
            }
            catch (Exception ex)
            {
                LogMsg(CommLogEventArgs.LogCategory.Error, $"Init midi in failed: {ex.Message}");
                ret = false;
            }

            LogMsg(CommLogEventArgs.LogCategory.Info, $"****** Midi In!");

            return ret;
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
                                ChannelNumber = evt.Channel,
                                NoteNumber = Utils.Constrain(evt.NoteNumber, Caps.MinNote, Caps.MaxNote),
                                Velocity = 0
                            };
                        }
                        else
                        {
                            step = new StepNoteOn()
                            {
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
                            ChannelNumber = evt.Channel,
                            NoteNumber = Utils.Constrain(evt.NoteNumber, Caps.MinNote, Caps.MaxNote),
                            Velocity = evt.Velocity
                        };
                    }
                    break;

                case MidiCommandCode.ControlChange:
                    {
                        ControlChangeEvent evt = me as ControlChangeEvent;
                        step = new StepControllerChange()
                        {
                            ChannelNumber = evt.Channel,
                            ControllerId = (int)evt.Controller,
                            Value = (byte)evt.ControllerValue
                        };
                    }
                    break;

                case MidiCommandCode.PitchWheelChange:
                    {
                        PitchWheelChangeEvent evt = me as PitchWheelChangeEvent;
                        step = new StepControllerChange()
                        {
                            ChannelNumber = evt.Channel,
                            ControllerId = ScriptDefinitions.TheDefinitions.PitchControl,
                            Value = Utils.Constrain(evt.Pitch, Caps.MinPitchValue, Caps.MaxPitchValue)
                        };
                    }
                    break;
            }

            if (step != null)
            {
                // Pass it up for handling.
                CommInputEventArgs args = new CommInputEventArgs() { Step = step };
                CommInputEvent?.Invoke(this, args);

                if (Monitor)
                {
                    LogMsg(CommLogEventArgs.LogCategory.Recv, step.ToString());
                }
            }
        }

        /// <summary>
        /// Process error midi event - Parameter 1 is invalid.
        /// </summary>
        void MidiIn_ErrorReceived(object sender, MidiInMessageEventArgs e)
        {
            if (Monitor)
            {
                LogMsg(CommLogEventArgs.LogCategory.Error, $"Message:0x{e.RawMessage:X8}");
            }
        }

        /// <summary>Ask host to do something with this.</summary>
        /// <param name="cat"></param>
        /// <param name="msg"></param>
        void LogMsg(CommLogEventArgs.LogCategory cat, string msg)
        {
            CommLogEvent?.Invoke(this, new CommLogEventArgs() { Category = cat, Message = msg });
        }
        #endregion
    }
}
