﻿using System;
using System.Collections.Generic;
using NAudio.Midi;
using Nebulator.Common;
using Nebulator.Comm;


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
        public event EventHandler<CommInputEventArgs> CommInputEvent;

        /// <inheritdoc />
        public event EventHandler<CommLogEventArgs> CommLogEvent;
        #endregion

        #region Properties
        /// <inheritdoc />
        public string CommName { get; private set; } = Utils.UNKNOWN_STRING;

        /// <inheritdoc />
        public CommCaps Caps { get; private set; } = null;

        /// <inheritdoc />
        public bool Inited { get; private set; } = false;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MidiInput()
        {
        }

        /// <inheritdoc />
        public bool Construct(string name)
        {
            CommName = name;
            return true;
        }

        /// <inheritdoc />
        public bool Init()
        {
            Caps = MidiUtils.GetCommCaps();
            Inited = false;

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
                }
                else
                {
                    _midiIn = new MidiIn(ind);
                    _midiIn.MessageReceived += MidiIn_MessageReceived;
                    _midiIn.ErrorReceived += MidiIn_ErrorReceived;
                    _midiIn.Start();
                    Inited = true;
                }
            }
            catch (Exception ex)
            {
                LogMsg(CommLogEventArgs.LogCategory.Error, $"Init midi in failed: {ex.Message}");
            }

            return Inited;
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
                                Comm = this,
                                ChannelNumber = evt.Channel,
                                NoteNumber = Utils.Constrain(evt.NoteNumber, Caps.MinNote, Caps.MaxNote),
                                Velocity = 0
                            };
                        }
                        else
                        {
                            step = new StepNoteOn()
                            {
                                Comm = this,
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
                            Comm = this,
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
                            Comm = this,
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
                            Comm = this,
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
                LogMsg(CommLogEventArgs.LogCategory.Recv, step.ToString());
            }
        }

        /// <summary>
        /// Process error midi event - Parameter 1 is invalid.
        /// </summary>
        void MidiIn_ErrorReceived(object sender, MidiInMessageEventArgs e)
        {
            LogMsg(CommLogEventArgs.LogCategory.Error, $"Message:0x{e.RawMessage:X8}");
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
