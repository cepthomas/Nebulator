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
    public class MidiOutput : NOutput
    {
        #region Fields
        /// <summary>Midi output device.</summary>
        MidiOut _midiOut = null;

        /// <summary>Midi access synchronizer.</summary>
        object _midiLock = new object();

        /// <summary>Notes to stop later.</summary>
        List<StepNoteOff> _stops = new List<StepNoteOff>();

        /// <summary>Resource clean up.</summary>
        bool _disposed = false;
        #endregion

        #region Events
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
        public MidiOutput()
        {
        }

        /// <inheritdoc />
        public bool Create(string name)
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
                if (_midiOut != null)
                {
                    _midiOut.Dispose();
                    _midiOut = null;
                }

                // Figure out which device.
                List<string> devices = new List<string>();
                for (int device = 0; device < MidiOut.NumberOfDevices; device++)
                {
                    devices.Add(MidiOut.DeviceInfo(device).ProductName);
                }

                int ind = devices.IndexOf(CommName);

                if (ind < 0)
                {
                    LogMsg(CommLogEventArgs.LogCategory.Error, $"Invalid midi: {CommName}");
                }
                else
                {
                    _midiOut = new MidiOut(ind);
                    Inited = true;
                }
            }
            catch (Exception ex)
            {
                LogMsg(CommLogEventArgs.LogCategory.Error, $"Init midi out failed: {ex.Message}");
                Inited = false;
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
            lock (_midiLock)
            {
                if(_midiOut != null)
                {
                    List<int> msgs = new List<int>();
                    int msg = 0;

                    switch (step)
                    {
                        case StepNoteOn stt:
                            {
                                NoteEvent evt = new NoteEvent(0, stt.ChannelNumber, MidiCommandCode.NoteOn, 
                                    Utils.Constrain(stt.NoteNumber, Caps.MinNote, Caps.MaxNote),
                                    Utils.Constrain(stt.VelocityToPlay, Caps.MinVolume, Caps.MaxVolume));
                                msg = evt.GetAsShortMessage();

                                if(stt.Duration.TotalTocks > 0) // specific duration
                                {
                                    // Remove any lingering note offs and add a fresh one.
                                    _stops.RemoveAll(s => s.NoteNumber == stt.NoteNumber && s.ChannelNumber == stt.ChannelNumber);

                                    _stops.Add(new StepNoteOff()
                                    {
                                        Comm = stt.Comm,
                                        ChannelNumber = stt.ChannelNumber,
                                        NoteNumber = Utils.Constrain(stt.NoteNumber, Caps.MinNote, Caps.MaxNote),
                                        Expiry = stt.Duration.TotalTocks
                                    });
                                }
                            }
                            break;

                        case StepNoteOff stt:
                            {
                                NoteEvent evt = new NoteEvent(0, stt.ChannelNumber, MidiCommandCode.NoteOff,
                                    Utils.Constrain(stt.NoteNumber, Caps.MinNote, Caps.MaxNote),
                                    Utils.Constrain(stt.Velocity, Caps.MinVolume, Caps.MaxVolume));
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
                                    PitchWheelChangeEvent pevt = new PitchWheelChangeEvent(0, stt.ChannelNumber,
                                        Utils.Constrain(stt.Value, Caps.MinPitchValue, Caps.MaxPitchValue));
                                    msg = pevt.GetAsShortMessage();
                                }
                                else // CC
                                {
                                    ControlChangeEvent nevt = new ControlChangeEvent(0, stt.ChannelNumber, (MidiController)stt.ControllerId,
                                        Utils.Constrain(stt.Value, Caps.MinControllerValue, Caps.MaxControllerValue));
                                    msg = nevt.GetAsShortMessage();
                                }
                            }
                            break;

                        case StepPatch stt:
                            {
                                PatchChangeEvent evt = new PatchChangeEvent(0, stt.ChannelNumber, stt.PatchNumber);
                                msg = evt.GetAsShortMessage();
                            }
                            break;

                        default:
                            break;
                    }

                    if(msg != 0)
                    {
                        try
                        {
                            _midiOut.Send(msg);
                            LogMsg(CommLogEventArgs.LogCategory.Send, step.ToString());
                        }
                        catch (Exception ex)
                        {
                            LogMsg(CommLogEventArgs.LogCategory.Error, $"Midi couldn't send step {step}: {ex.Message}");
                            ret = false;
                        }
                    }
                }
            }

            return ret;
        }

        /// <inheritdoc />
        public void KillAll()
        {
            for (int i = 0; i < Caps.NumChannels; i++)
            {
                Kill(i + 1);
            }
        }

        /// <inheritdoc />
        public void Kill(int? channel)
        {
            if(channel is null)
            {
                if(Inited)
                {
                    for (int i = 0; i < Caps.NumChannels; i++)
                    {
                        Send(new StepControllerChange()
                        {
                            Comm = this,
                            ChannelNumber = i + 1,
                            ControllerId = (int)MidiController.AllNotesOff
                        });
                    }
                }
            }
            else
            {
                Send(new StepControllerChange()
                {
                    Comm = this,
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
        void LogMsg(CommLogEventArgs.LogCategory cat, string msg)
        {
            CommLogEvent?.Invoke(this, new CommLogEventArgs() { Category = cat, Message = msg });
        }
        #endregion
    }
}
