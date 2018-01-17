using System;
using System.Collections.Generic;
using NAudio.Midi;
using NLog;
using Nebulator.Common;


namespace Nebulator.Midi
{
    /// <summary>
    /// Abstraction layer between NAudio midi and Nebulator steps.
    /// </summary>
    public class MidiInterface : IDisposable
    {
        /// <summary>The one and only midi in/out devices.</summary>
        public static MidiInterface TheInterface { get; set; } = new MidiInterface();

        #region Definitions
        // We borrow a few unused midi controller numbers for internal use.
        // Currently undefined: 3, 9, 14, 15, 20-31, 35, 41, 46, 47, 52-63, 85-87, 89, 90 and 102-119.
        public const int CTRL_NONE = 3;
        public const int CTRL_PITCH = 9; // TODO2 Is this semi-kludge the best way to handle pitch?

        public const int NUM_MIDI_CHANNELS = 16;
        public const int MAX_MIDI_NOTE = 127;
        public const int MAX_MIDI_VOLUME = 127;
        public const int MAX_MIDI_CTRL_VALUE = 127;
        public const int MAX_MIDI_PITCH_VALUE = 16383;

        public const int MIDDLE_C = 60;
        #endregion


        #region Fields
        /// <summary>My logger.</summary>
        Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>Midi input device.</summary>
        MidiIn _midiIn = null;

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
        /// <summary>Reporting a change to listeners.</summary>
        public event EventHandler<NebMidiInputEventArgs> NebMidiInputEvent;

        public class NebMidiInputEventArgs : EventArgs
        {
            /// <summary>Received data.</summary>
            public Step Step { get; set; } = null;
        }

        /// <summary>Request for logging service.</summary>
        public event EventHandler<NebMidiLogEventArgs> NebMidiLogEvent;
        public class NebMidiLogEventArgs : EventArgs
        {
            /// <summary>Something to log.</summary>
            public string Message { get; set; } = null;
        }
        #endregion

        #region Properties
        /// <summary>All available midi inputs for UI selection.</summary>
        public List<string> MidiInputs { get; set; } = new List<string>();

        /// <summary>All available midi outputs for UI selection.</summary>
        public List<string> MidiOutputs { get; set; } = new List<string>();
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MidiInterface()
        {
        }

        /// <summary>
        /// Initialize everything.
        /// </summary>
        public void Init()
        {
            InitMidiIn();
            InitMidiOut();
        }

        /// <summary>
        /// Start listening for midi inputs.
        /// </summary>
        public void Start()
        {
            _midiIn?.Start();
        }

        /// <summary>
        /// Stop listening for midi inputs.
        /// </summary>
        public void Stop()
        {
            _midiIn?.Stop();
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

                _midiOut?.Dispose();
                _midiOut = null;

                _disposed = true;
            }
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Process any stop notes.
        /// </summary>
        public void Housekeep()
        {
            // Send any stops due.
            _stops.ForEach(s => { s.Expiry--; if (s.Expiry < 0) Send(s); });
            
            // Reset.
            _stops.RemoveAll(s => s.Expiry < 0);
        }

        /// <summary>
        /// Convert from NAudio def to Neb.
        /// </summary>
        /// <param name="sctlr"></param>
        /// <returns></returns>
        public static int TranslateController(string sctlr)
        {
            MidiController ctlr = (MidiController)Enum.Parse(typeof(MidiController), sctlr);
            return (int)ctlr;
        }
        #endregion

        #region Midi I/O
        /// <summary>
        /// Midi out processor.
        /// </summary>
        /// <param name="step"></param>
        /// <param name="chase">Let midi output generate the note off.</param>
        public void Send(Step step, bool chase = false)
        {
            // Critical code section
            lock (_midiLock)
            {
                if(_midiOut != null)
                {
                    int msg = 0;

                    switch (step)
                    {
                        case StepNoteOn stt:
                            {
                                NoteEvent evt = new NoteEvent(0, stt.Channel, MidiCommandCode.NoteOn, 
                                    Utils.Constrain(stt.NoteNumberToPlay, 0, MAX_MIDI_NOTE),
                                    Utils.Constrain(stt.VelocityToPlay, 0, MAX_MIDI_VOLUME));
                                msg = evt.GetAsShortMessage();

                                if(chase)
                                {
                                    // Remove any lingering note offs.
                                    _stops.RemoveAll(s => s.NoteNumber == stt.NoteNumber);
                                    _stops.Add(new StepNoteOff(stt));
                                }
                            }
                            break;

                        case StepNoteOff stt:
                            {
                                NoteEvent evt = new NoteEvent(0, stt.Channel, MidiCommandCode.NoteOff,
                                    Utils.Constrain(stt.NoteNumberToPlay, 0, MAX_MIDI_NOTE),
                                    Utils.Constrain(stt.Velocity, 0, MAX_MIDI_VOLUME));
                                msg = evt.GetAsShortMessage();
                            }
                            break;

                        case StepControllerChange stt:
                            {
                                if (stt.MidiController == CTRL_PITCH) // hacked in pitch support
                                {
                                    PitchWheelChangeEvent evt = new PitchWheelChangeEvent(0, stt.Channel,
                                        Utils.Constrain(stt.ControllerValue, 0, MAX_MIDI_PITCH_VALUE));
                                    msg = evt.GetAsShortMessage();
                                }
                                else // plain controller
                                {
                                    ControlChangeEvent evt = new ControlChangeEvent(0, stt.Channel, (MidiController)stt.MidiController,
                                        Utils.Constrain(stt.ControllerValue, 0, MAX_MIDI_CTRL_VALUE));
                                    msg = evt.GetAsShortMessage();
                                }
                            }
                            break;

                        case StepPatch stt:
                            {
                                PatchChangeEvent evt = new PatchChangeEvent(0, stt.Channel, stt.PatchNumber);
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

                            if (Globals.TheSettings.MidiMonitorOut)
                            {
                                NebMidiLogEvent?.Invoke(this, new NebMidiLogEventArgs() { Message = $"SND: {step}" });
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"Midi couldn't send step {step}: {ex.Message}");
                        }
                    }
                }
            }
        }

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
                                Channel = evt.Channel,
                                NoteNumber = Utils.Constrain(evt.NoteNumber, 0, MAX_MIDI_NOTE),
                                Velocity = 0
                            };

                        }
                        else
                        {
                            step = new StepNoteOn()
                            {
                                Channel = evt.Channel,
                                NoteNumber = evt.NoteNumber,
                                NoteNumberToPlay = evt.NoteNumber,
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
                            Channel = evt.Channel,
                            NoteNumber = Utils.Constrain(evt.NoteNumber, 0, MAX_MIDI_NOTE),
                            Velocity = evt.Velocity
                        };
                    }
                    break;

                case MidiCommandCode.ControlChange:
                    {
                        ControlChangeEvent evt = me as ControlChangeEvent;
                        step = new StepControllerChange()
                        {
                            Channel = evt.Channel,
                            MidiController = (int)evt.Controller,
                            ControllerValue = (byte)evt.ControllerValue
                        };
                    }
                    break;

                case MidiCommandCode.PitchWheelChange:
                    {
                        PitchWheelChangeEvent evt = me as PitchWheelChangeEvent;
                        step = new StepControllerChange()
                        {
                            Channel = evt.Channel,
                            MidiController = CTRL_PITCH,
                            ControllerValue = evt.Pitch
                        };
                    }
                    break;
            }

            if (step != null)
            {
                if(step is StepNoteOn || step is StepNoteOff)
                {
                    // Pass through. TODO2 or do something useful with it, similar to _ctrlChanges. Map ranges of notes to different things.
                    Send(step);
                }
                else
                {
                    // Pass it up for handling.
                    NebMidiInputEvent?.Invoke(this, new NebMidiInputEventArgs() { Step = step });
                }

                if (Globals.TheSettings.MidiMonitorIn)
                {
                    NebMidiLogEvent?.Invoke(this, new NebMidiLogEventArgs() { Message = $"RCV: {step}" });
                }
            }
        }

        /// <summary>
        /// Process error midi event - Parameter 1 is invalid.
        /// </summary>
        void MidiIn_ErrorReceived(object sender, MidiInMessageEventArgs e)
        {
            if (Globals.TheSettings.MidiMonitorIn)
            {
                NebMidiLogEvent?.Invoke(this, new NebMidiLogEventArgs() { Message = $"ERR: Message:0x{e.RawMessage:X8}" });
            }
        }

        /// <summary>
        /// Set up midi in.
        /// </summary>
        void InitMidiIn()
        {
            try
            {
                if (_midiIn != null)
                {
                    _midiIn.Stop();
                    _midiIn.Dispose();
                    _midiIn = null;
                }

                MidiInputs.Clear();
                for (int device = 0; device < MidiIn.NumberOfDevices; device++)
                {
                    MidiInputs.Add(MidiIn.DeviceInfo(device).ProductName);
                }

                if (MidiInputs.Count > 0 && MidiInputs.Contains(Globals.TheSettings.MidiIn))
                {
                    _midiIn = new MidiIn(MidiInputs.IndexOf(Globals.TheSettings.MidiIn));
                    _midiIn.MessageReceived += MidiIn_MessageReceived;
                    _midiIn.ErrorReceived += MidiIn_ErrorReceived;
                    _midiIn.Start();
                }
                else
                {
                    NebMidiLogEvent?.Invoke(this, new NebMidiLogEventArgs() { Message = $"INF: No midi input device selected." });
                }
            }
            catch (Exception ex)
            {
                NebMidiLogEvent?.Invoke(this, new NebMidiLogEventArgs() { Message = $"ERR: Init midi in failed: {ex.Message}" });
            }
        }

        /// <summary>
        /// Set up midi out.
        /// </summary>
        void InitMidiOut()
        {
            try
            {
                if (_midiOut != null)
                {
                    _midiOut.Dispose();
                    _midiOut = null;
                }

                MidiOutputs.Clear();
                for (int device = 0; device < MidiOut.NumberOfDevices; device++)
                {
                    MidiOutputs.Add(MidiOut.DeviceInfo(device).ProductName);
                }

                if (MidiOutputs.Count > 0 && MidiOutputs.Contains(Globals.TheSettings.MidiOut))
                {
                    int mi = MidiOutputs.IndexOf(Globals.TheSettings.MidiOut);
                    _midiOut = new MidiOut(mi);
                    //_midiOut.Volume = -1; needs to be this
                }
                else
                {
                    NebMidiLogEvent?.Invoke(this, new NebMidiLogEventArgs() { Message = $"ERR: No midi output device selected." });
                }
            }
            catch (Exception ex)
            {
                NebMidiLogEvent?.Invoke(this, new NebMidiLogEventArgs() { Message = $"ERR: Init midi out failed: {ex.Message}" });
            }
        }

        /// <summary>
        /// Kill all midi channels.
        /// </summary>
        public void KillAll()
        {
            for (int i = 0; i < NUM_MIDI_CHANNELS; i++)
            {
                Kill(i + 1);
            }
        }

        /// <summary>
        /// Kill one midi channel.
        /// </summary>
        public void Kill(int channel)
        {
            StepControllerChange step = new StepControllerChange()
            {
                Channel = channel,
                MidiController = (int)MidiController.AllNotesOff
            };
            Send(step);
        }
        #endregion
    }
}
