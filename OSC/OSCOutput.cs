using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Threading;
using System.Drawing;
using Nebulator.Common;
using Nebulator.Comm;


namespace Nebulator.OSC
{
    /// <summary>
    /// Abstraction layer between OSC comm and Nebulator steps.
    /// </summary>
    public class OSCOutput : NOutput
    {
        #region Fields
        /// <summary>OSC output device.</summary>
        UdpClient _udpClient;

        ///// <summary>Access synchronizer.</summary>
        //object _oscLock = new object();

        ///// <summary>Notes to stop later.</summary>
        //List<StepNoteOff> _stops = new List<StepNoteOff>();

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

        /// <summary>Where to?</summary>
        public string ServerIP { get; private set; } = Utils.UNKNOWN_STRING;

        /// <summary>Where to?</summary>
        public int ServerPort { get; private set; } = -1;

        /// <inheritdoc />
        public CommCaps Caps { get; private set; } = OSCUtils.InitCaps();

        /// <inheritdoc />
        public bool Inited { get; private set; } = false;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public OSCOutput()
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
                try
            {
                if (_udpClient != null)
                {
                    _udpClient.Close();
                    _udpClient.Dispose();
                    _udpClient = null;
                }

                _udpClient = new UdpClient(ServerIP, ServerPort);

                //// Figure out which device.
                //List<string> devices = new List<string>();
                //for (int device = 0; device < MidiOut.NumberOfDevices; device++)
                //{
                //    devices.Add(MidiOut.DeviceInfo(device).ProductName);
                //}

                //int ind = devices.IndexOf(CommName);

                //if (ind < 0)
                //{
                //    LogMsg(CommLogEventArgs.LogCategory.Error, $"Invalid midi: {CommName}");
                //}
                //else
                //{
                //    _udpClient = new UdpClient(_remoteHost, _remotePort);
                //    Inited = true;
                //}

                Inited = true;
            }
            catch (Exception ex)
            {
                LogMsg(CommLogEventArgs.LogCategory.Error, $"Init OSC out failed: {ex.Message}");
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
                _udpClient?.Close();
                _udpClient?.Dispose();
                _udpClient = null;

                _disposed = true;
            }
        }
        #endregion

        #region Public functions
        /// <inheritdoc />
        public void Housekeep()
        {
            //// Send any stops due.
            //_stops.ForEach(s => { s.Expiry--; if (s.Expiry < 0) Send(s); });

            //// Reset.
            //_stops.RemoveAll(s => s.Expiry < 0);
        }

        /// <inheritdoc />
        public bool Send(Step step)
        {
            bool ret = true;

            //// Critical code section.
            //lock (_midiLock)
            //{
            //    if (_midiOut != null)
            //    {
            //        List<int> msgs = new List<int>();
            //        int msg = 0;

            //        switch (step)
            //        {
            //            case StepNoteOn stt:
            //                {
            //                    NoteEvent evt = new NoteEvent(0, stt.ChannelNumber, MidiCommandCode.NoteOn,
            //                        Utils.Constrain(stt.NoteNumber, Caps.MinNote, Caps.MaxNote),
            //                        Utils.Constrain(stt.VelocityToPlay, Caps.MinVolume, Caps.MaxVolume));
            //                    msg = evt.GetAsShortMessage();

            //                    if (stt.Duration.TotalTocks > 0) // specific duration
            //                    {
            //                        // Remove any lingering note offs and add a fresh one.
            //                        _stops.RemoveAll(s => s.NoteNumber == stt.NoteNumber && s.ChannelNumber == stt.ChannelNumber);

            //                        _stops.Add(new StepNoteOff()
            //                        {
            //                            Comm = stt.Comm,
            //                            ChannelNumber = stt.ChannelNumber,
            //                            NoteNumber = Utils.Constrain(stt.NoteNumber, Caps.MinNote, Caps.MaxNote),
            //                            Expiry = stt.Duration.TotalTocks
            //                        });
            //                    }
            //                }
            //                break;

            //            case StepNoteOff stt:
            //                {
            //                    NoteEvent evt = new NoteEvent(0, stt.ChannelNumber, MidiCommandCode.NoteOff,
            //                        Utils.Constrain(stt.NoteNumber, Caps.MinNote, Caps.MaxNote),
            //                        Utils.Constrain(stt.Velocity, Caps.MinVolume, Caps.MaxVolume));
            //                    msg = evt.GetAsShortMessage();
            //                }
            //                break;

            //            case StepControllerChange stt:
            //                {
            //                    if (stt.ControllerId == ScriptDefinitions.TheDefinitions.NoteControl)
            //                    {
            //                        // Shouldn't happen, ignore.
            //                    }
            //                    else if (stt.ControllerId == ScriptDefinitions.TheDefinitions.PitchControl)
            //                    {
            //                        PitchWheelChangeEvent pevt = new PitchWheelChangeEvent(0, stt.ChannelNumber,
            //                            Utils.Constrain(stt.Value, Caps.MinPitchValue, Caps.MaxPitchValue));
            //                        msg = pevt.GetAsShortMessage();
            //                    }
            //                    else // CC
            //                    {
            //                        ControlChangeEvent nevt = new ControlChangeEvent(0, stt.ChannelNumber, (MidiController)stt.ControllerId,
            //                            Utils.Constrain(stt.Value, Caps.MinControllerValue, Caps.MaxControllerValue));
            //                        msg = nevt.GetAsShortMessage();
            //                    }
            //                }
            //                break;

            //            case StepPatch stt:
            //                {
            //                    PatchChangeEvent evt = new PatchChangeEvent(0, stt.ChannelNumber, stt.PatchNumber);
            //                    msg = evt.GetAsShortMessage();
            //                }
            //                break;

            //            default:
            //                break;
            //        }

            //        if (msg != 0)
            //        {
            //            try
            //            {
            //                _midiOut.Send(msg);
            //                LogMsg(CommLogEventArgs.LogCategory.Send, step.ToString());
            //            }
            //            catch (Exception ex)
            //            {
            //                LogMsg(CommLogEventArgs.LogCategory.Error, $"Midi couldn't send step {step}: {ex.Message}");
            //                ret = false;
            //            }
            //        }
                //}
            //}

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
            if (channel is null)
            {
                if (Inited)
                {
                    for (int i = 0; i < Caps.NumChannels; i++)
                    {
                        Send(new StepControllerChange()
                        {
                            Comm = this,
                            ChannelNumber = i + 1,
 //                           ControllerId = (int)MidiController.AllNotesOff
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
 //                   ControllerId = (int)MidiController.AllNotesOff
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



        // TODOX all these:
        Bundle Format(List<Step> steps)
        {
            Bundle bundle = new Bundle(new TimeTag());

            return bundle;
        }

        Message Format(Step step)
        {
            Message message = new Message("TODOX");

            return message;
        }

        public int Send(Bundle packet)
        {
            int byteNum = 0;
            byte[] data = null;

            try
            {
                byteNum = _udpClient.Send(data, data.Length);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
            }

            return byteNum;
        }



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
