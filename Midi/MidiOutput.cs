using System;
using System.Collections.Generic;
using NAudio.Midi;
using NBagOfTricks;
using Nebulator.Common;
using Nebulator.Device;


namespace Nebulator.Midi
{
    /// <summary>
    /// Abstraction layer between NAudio midi and Nebulator steps.
    /// </summary>
    public class MidiOutput : NOutput
    {
        #region Fields
        /// <summary>Midi output device.</summary>
        MidiOut _midiOut = null;

        /// <summary>Midi access synchronizer.</summary>
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
        public MidiOutput()
        {
        }

        /// <inheritdoc />
        public bool Init(string name)
        {
            bool inited = false;

            DeviceName = "Invalid"; // default

            try
            {
                if (_midiOut != null)
                {
                    _midiOut.Dispose();
                    _midiOut = null;
                }

                List<string> parts = name.SplitByToken(":");
                if(parts.Count == 2)
                {
                    // Figure out which device.
                    List<string> devices = new List<string>();
                    for (int device = 0; device < MidiOut.NumberOfDevices; device++)
                    {
                        devices.Add(MidiOut.DeviceInfo(device).ProductName);
                    }

                    int ind = devices.IndexOf(parts[1]);

                    if (ind < 0)
                    {
                        LogMsg(DeviceLogCategory.Error, $"Invalid midi: {parts[1]}");
                    }
                    else
                    {
                        _midiOut = new MidiOut(ind);
                        inited = true;
                        DeviceName = parts[1];
                    }
                }
            }
            catch (Exception ex)
            {
                LogMsg(DeviceLogCategory.Error, $"Init midi out failed: {ex.Message}");
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
                _midiOut?.Dispose();
                _midiOut = null;

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
                if(_midiOut != null)
                {
                    List<int> msgs = new List<int>();
                    int msg = 0;

                    switch (step)
                    {
                        case StepNoteOn stt:
                            {
                                NoteEvent evt = new NoteEvent(0,
                                    stt.ChannelNumber,
                                    MidiCommandCode.NoteOn,
                                    (int)MathUtils.Constrain(stt.NoteNumber, 0, MidiUtils.MAX_MIDI),
                                    (int)(MathUtils.Constrain(stt.VelocityToPlay, 0, 1.0) * MidiUtils.MAX_MIDI));
                                msg = evt.GetAsShortMessage();

                                if (stt.Duration.TotalIncrs > 0) // specific duration
                                {
                                    // Remove any lingering note offs and add a fresh one.
                                    _stops.RemoveAll(s => s.NoteNumber == stt.NoteNumber && s.ChannelNumber == stt.ChannelNumber);

                                    _stops.Add(new StepNoteOff()
                                    {
                                        Device = stt.Device,
                                        ChannelNumber = stt.ChannelNumber,
                                        NoteNumber = MathUtils.Constrain(stt.NoteNumber, 0, MidiUtils.MAX_MIDI),
                                        Expiry = stt.Duration.TotalIncrs
                                    });
                                }
                            }
                            break;

                        case StepNoteOff stt:
                            {
                                NoteEvent evt = new NoteEvent(0,
                                    stt.ChannelNumber,
                                    MidiCommandCode.NoteOff,
                                    (int)MathUtils.Constrain(stt.NoteNumber, 0, MidiUtils.MAX_MIDI),
                                    0);
                                msg = evt.GetAsShortMessage();
                            }
                            break;

                        case StepControllerChange stt:
                            {
                                if (stt.ControllerId == ScriptDefinitions.TheDefinitions.NoteControl)
                                {
                                    // Shouldn't happen, ignore.
                                }
                                else if (stt.ControllerId == ScriptDefinitions.TheDefinitions.PitchControl)
                                {
                                    PitchWheelChangeEvent pevt = new PitchWheelChangeEvent(0,
                                        stt.ChannelNumber,
                                        (int)MathUtils.Constrain(stt.Value, 0, MidiUtils.MAX_PITCH));
                                    msg = pevt.GetAsShortMessage();
                                }
                                else // CC
                                {
                                    ControlChangeEvent nevt = new ControlChangeEvent(0,
                                        stt.ChannelNumber,
                                        (MidiController)stt.ControllerId,
                                        (int)MathUtils.Constrain(stt.Value, 0, MidiUtils.MAX_MIDI));
                                    msg = nevt.GetAsShortMessage();
                                }
                            }
                            break;

                        case StepPatch stt:
                            {
                                PatchChangeEvent evt = new PatchChangeEvent(0,
                                    stt.ChannelNumber,
                                    stt.PatchNumber);
                                msg = evt.GetAsShortMessage();
                            }
                            break;

                        default:
                            break;
                    }

                    if(msg != 0)
                    {
                        _midiOut.Send(msg);
                        LogMsg(DeviceLogCategory.Send, step.ToString());
                    }
                }
            }

            return ret;
        }

        /// <inheritdoc />
        public void Kill(int? channel)
        {
            if(channel is null)
            {
                for (int i = 0; i < MidiUtils.MAX_CHANNELS; i++)
                {
                    Send(new StepControllerChange()
                    {
                        Device = this,
                        ChannelNumber = i + 1,
                        ControllerId = (int)MidiController.AllNotesOff
                    });
                }
            }
            else
            {
                Send(new StepControllerChange()
                {
                    Device = this,
                    ChannelNumber = channel.Value,
                    ControllerId = (int)MidiController.AllNotesOff
                });
            }
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
