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
    public class MidiOutput : IOutputDevice
    {
        #region Fields
        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.GetLogger("MidiOutput");

        /// <summary>Midi output device.</summary>
        MidiOut? _midiOut = null;

        /// <summary>Midi access synchronizer.</summary>
        readonly object _lock = new();

        /// <summary>Notes to stop later.</summary>
        readonly List<StepNoteOff> _stops = new();

        /// <summary>Resource clean up.</summary>
        bool _disposed = false;
        #endregion

        #region Properties
        /// <inheritdoc />
        public string DeviceName { get; private set; } = Definitions.UNKNOWN_STRING;

        /// <inheritdoc />
        public DeviceType DeviceType => DeviceType.MidiOut;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MidiOutput()
        {
        }

        /// <inheritdoc />
        public bool Init()
        {
            bool inited = false;

            DeviceName = "Invalid"; // default

            try
            {
                _midiOut?.Dispose();
                _midiOut = null;

                // Figure out which device.
                List<string> devices = new();
                for (int i = 0; i < MidiOut.NumberOfDevices; i++)
                {
                    devices.Add(MidiOut.DeviceInfo(i).ProductName);
                }

                int ind = devices.IndexOf(UserSettings.TheSettings.MidiOut);

                if (ind < 0)
                {
                    _logger.Error($"Invalid midi output device:{UserSettings.TheSettings.MidiOut}");
                }
                else
                {
                    _midiOut = new MidiOut(ind);
                    inited = true;
                    DeviceName = UserSettings.TheSettings.MidiOut;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Init midi out failed: {ex.Message}");
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
                if(_midiOut is not null)
                {
                    List<int> msgs = new();
                    int msg = 0;

                    switch (step)
                    {
                        case StepNoteOn stt:
                            {
                                NoteEvent evt = new(0,
                                    stt.ChannelNumber,
                                    MidiCommandCode.NoteOn,
                                    (int)MathUtils.Constrain(stt.NoteNumber, 0, Definitions.MAX_MIDI),
                                    (int)(MathUtils.Constrain(stt.VelocityToPlay, 0, 1.0) * Definitions.MAX_MIDI));
                                msg = evt.GetAsShortMessage();

                                if (stt.Duration.TotalSubdivs > 0) // specific duration
                                {
                                    // Remove any lingering note offs and add a fresh one.
                                    _stops.RemoveAll(s => s.NoteNumber == stt.NoteNumber && s.ChannelNumber == stt.ChannelNumber);

                                    _stops.Add(new()
                                    {
                                        Device = stt.Device,
                                        ChannelNumber = stt.ChannelNumber,
                                        NoteNumber = MathUtils.Constrain(stt.NoteNumber, 0, Definitions.MAX_MIDI),
                                        Expiry = stt.Duration.TotalSubdivs
                                    });
                                }
                            }
                            break;

                        case StepNoteOff stt:
                            {
                                NoteEvent evt = new(0,
                                    stt.ChannelNumber,
                                    MidiCommandCode.NoteOff,
                                    (int)MathUtils.Constrain(stt.NoteNumber, 0, Definitions.MAX_MIDI),
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
                                    PitchWheelChangeEvent pevt = new(0,
                                        stt.ChannelNumber,
                                        (int)MathUtils.Constrain(stt.Value, 0, MidiUtils.MAX_PITCH));
                                    msg = pevt.GetAsShortMessage();
                                }
                                else // CC
                                {
                                    ControlChangeEvent nevt = new(0,
                                        stt.ChannelNumber,
                                        (MidiController)stt.ControllerId,
                                        (int)MathUtils.Constrain(stt.Value, 0, Definitions.MAX_MIDI));
                                    msg = nevt.GetAsShortMessage();
                                }
                            }
                            break;

                        case StepPatch stt:
                            {
                                PatchChangeEvent evt = new(0,
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
                        if(UserSettings.TheSettings.MonitorOutput)
                        {
                            _logger.Trace($"{TraceCat.SND} MidiOut:{step}");
                        }
                    }
                    else
                    {
                        _logger.Error($"Send failed");
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
                for (int i = 0; i < Channel.NUM_CHANNELS; i++)
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
    }
}
