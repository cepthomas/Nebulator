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
    public class OSCInput : NInput
    {
        #region Fields
        /// <summary>Midi input device.</summary>
        UdpClient _udpClient = null;

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

        /// <summary>The local port.</summary>
        public int ClientPort { get; set; } = -1;

        /// <inheritdoc />
        public CommCaps Caps { get; private set; } = OSCUtils.InitCaps();

        /// <inheritdoc />
        public bool Inited { get; private set; } = false;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public OSCInput()
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

                //// Figure out which device.
                //List<string> devices = new List<string>();
                //for (int device = 0; device < MidiIn.NumberOfDevices; device++)
                //{
                //    devices.Add(MidiIn.DeviceInfo(device).ProductName);
                //}

                //int ind = devices.IndexOf(CommName);

                //if(ind < 0)
                //{
                //    LogMsg(CommLogEventArgs.LogCategory.Error, $"Invalid midi: {CommName}");
                //}
                //else
                //{
                //    _midiIn = new MidiIn(ind);
                //    _midiIn.MessageReceived += MidiIn_MessageReceived;
                //    _midiIn.ErrorReceived += MidiIn_ErrorReceived;
                //    _midiIn.Start();
                //    Inited = true;
                //}

                _udpClient = new UdpClient(ClientPort);
                //udpClient.MessageReceived += MidiIn_MessageReceived;
                //udpClient.ErrorReceived += MidiIn_ErrorReceived;
                //_midiIn.Start();
                Inited = true;

            }
            catch (Exception ex)
            {
                LogMsg(CommLogEventArgs.LogCategory.Error, $"Init OSC in in failed: {ex.Message}");
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
        public void Start()
        {
           // _midiIn?.Start();
        }

        /// <inheritdoc />
        public void Stop()
        {
          //  _midiIn?.Stop();
        }

        /// <inheritdoc />
        public void Housekeep()
        {
        }
        #endregion

        #region Private functions
        public Bundle Receive() // needs a thread/async TODOX 
        {
            try
            {
                IPEndPoint ip = null;
                byte[] bytes = _udpClient.Receive(ref ip);

                if (bytes != null && bytes.Length > 0)
                {
                    // unpack - check for bundle or message TODOX nested bundles?
                    if(bytes[0]== '#')
                    {
                        Bundle b = Bundle.Parse(bytes);
                    }
                    else
                    {
                        Message m = Message.Parse(bytes);
                    }


                    //    // Decode the message. We only care about a few.
                    //    MidiEvent me = MidiEvent.FromRawMessage(e.RawMessage);
                    //    Step step = null;

                    //    switch (me.CommandCode)
                    //    {
                    //        case MidiCommandCode.NoteOn:
                    //            {
                    //                NoteOnEvent evt = me as NoteOnEvent;

                    //                if(evt.Velocity == 0)
                    //                {
                    //                    step = new StepNoteOff()
                    //                    {
                    //                        Comm = this,
                    //                        ChannelNumber = evt.Channel,
                    //                        NoteNumber = Utils.Constrain(evt.NoteNumber, Caps.MinNote, Caps.MaxNote),
                    //                        Velocity = 0
                    //                    };
                    //                }
                    //                else
                    //                {
                    //                    step = new StepNoteOn()
                    //                    {
                    //                        Comm = this,
                    //                        ChannelNumber = evt.Channel,
                    //                        NoteNumber = evt.NoteNumber,
                    //                        Velocity = evt.Velocity,
                    //                        VelocityToPlay = evt.Velocity,
                    //                        Duration = new Time(0)
                    //                    };
                    //                }
                    //            }
                    //            break;

                    //        case MidiCommandCode.NoteOff:
                    //            {
                    //                NoteEvent evt = me as NoteEvent;
                    //                step = new StepNoteOff()
                    //                {
                    //                    Comm = this,
                    //                    ChannelNumber = evt.Channel,
                    //                    NoteNumber = Utils.Constrain(evt.NoteNumber, Caps.MinNote, Caps.MaxNote),
                    //                    Velocity = evt.Velocity
                    //                };
                    //            }
                    //            break;

                    //        case MidiCommandCode.ControlChange:
                    //            {
                    //                ControlChangeEvent evt = me as ControlChangeEvent;
                    //                step = new StepControllerChange()
                    //                {
                    //                    Comm = this,
                    //                    ChannelNumber = evt.Channel,
                    //                    ControllerId = (int)evt.Controller,
                    //                    Value = evt.ControllerValue
                    //                };
                    //            }
                    //            break;

                    //        case MidiCommandCode.PitchWheelChange:
                    //            {
                    //                PitchWheelChangeEvent evt = me as PitchWheelChangeEvent;
                    //                step = new StepControllerChange()
                    //                {
                    //                    Comm = this,
                    //                    ChannelNumber = evt.Channel,
                    //                    ControllerId = ScriptDefinitions.TheDefinitions.PitchControl,
                    //                    Value = Utils.Constrain(evt.Pitch, Caps.MinPitchValue, Caps.MaxPitchValue)
                    //                };
                    //            }
                    //            break;
                    //    }

                    //    if (step != null)
                    //    {
                    //        // Pass it up for handling.
                    //        CommInputEventArgs args = new CommInputEventArgs() { Step = step };
                    //        CommInputEvent?.Invoke(this, args);
                    //        LogMsg(CommLogEventArgs.LogCategory.Recv, step.ToString());
                    //    }

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }

            return null;
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
