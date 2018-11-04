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
    public class OscInput : NInput
    {
        #region Fields
        /// <summary>OSC input device.</summary>
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
        public int Port { get; set; } = -1;

        /// <inheritdoc />
        public CommCaps Caps { get; private set; } = OscUtils.InitCaps();
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public OscInput()
        {
        }

        /// <inheritdoc />
        public bool Init(string name)
        {
            bool inited = false;

            CommName = name;

            try
            {
                if (_udpClient != null)
                {
                    _udpClient.Close();
                    _udpClient.Dispose();
                    _udpClient = null;
                }

                // Check for properly formed port.
                List<string> parts = name.SplitByToken(":");
                if (parts.Count == 2 && parts[0] == "OSC")
                {
                    if (int.TryParse(parts[1], out int port))
                    {
                        Port = port;
                        _udpClient = new UdpClient(Port);
                        inited = true;
                    }
                }
            }
            catch (Exception ex)
            {
                inited = false;
                LogMsg(CommLogEventArgs.LogCategory.Error, $"Init OSC in failed: {ex.Message}");
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
            _udpClient.BeginReceive(new AsyncCallback(OscReceive), this);
        }

        /// <inheritdoc />
        public void Stop()
        {
            _udpClient.Close();
        }

        /// <inheritdoc />
        public void Housekeep()
        {
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Handle a received message.
        /// </summary>
        /// <param name="ares"></param>
        void OscReceive(IAsyncResult ares)
        {
            //OscInput inputDev = ares.AsyncState as OscInput;
            IPEndPoint sender = new IPEndPoint(0, 0);

            // Process input.
            byte[] bytes = _udpClient.EndReceive(ares, ref sender);

            if (bytes != null && bytes.Length > 0)
            {
                // Unpack - check for bundle or message.
                if (bytes[0] == '#')
                {
                    Bundle b = Bundle.Unpack(bytes);
                    if(b.Errors.Count == 0)
                    {
                        b.Messages.ForEach(m => ProcessMessage(m));
                    }
                    else
                    {
                        b.Errors.ForEach(e => LogMsg(CommLogEventArgs.LogCategory.Error, e));
                    }
                }
                else
                {
                    Message m = Message.Unpack(bytes);
                    if (m.Errors.Count == 0)
                    {
                        ProcessMessage(m);
                    }
                    else
                    {
                        m.Errors.ForEach(e => LogMsg(CommLogEventArgs.LogCategory.Error, e));
                    }
                }
            }

            // Local message decoder.
            void ProcessMessage(Message msg)
            {
                // could be:
                // /note/ channel notenum vel
                // /control/ channel ctlnum val

                Step step = null;

                switch(msg.Address)
                {
                    case "/note/":
                        if(msg.Data.Count == 3)
                        {
                            int channel = Utils.Constrain((int)msg.Data[0], 0, Caps.NumChannels);
                            int notenum = Utils.Constrain((int)msg.Data[1], Caps.MinNote, Caps.MaxNote);
                            int velocity = Utils.Constrain((int)msg.Data[2], Caps.MinVolume, Caps.MaxVolume);

                            if (velocity == 0)
                            {
                                step = new StepNoteOff()
                                {
                                    Comm = this,
                                    ChannelNumber = channel,
                                    NoteNumber = notenum,
                                    Velocity = 0
                                };
                            }
                            else
                            {
                                step = new StepNoteOn()
                                {
                                    Comm = this,
                                    ChannelNumber = channel,
                                    NoteNumber = notenum,
                                    Velocity = velocity,
                                    VelocityToPlay = velocity,
                                    Duration = new Time(0)
                                };
                            }
                        }
                        break;

                    case "/control/":
                        if (msg.Data.Count == 3)
                        {
                            int channel = Utils.Constrain((int)msg.Data[0], 0, Caps.NumChannels);
                            int ctlnum = (int)msg.Data[1];
                            int value = Utils.Constrain((int)msg.Data[2], Caps.MinControllerValue, Caps.MaxControllerValue);

                            step = new StepControllerChange()
                            {
                                Comm = this,
                                ChannelNumber = channel,
                                ControllerId = ctlnum,
                                Value = value
                            };
                        }
                        break;

                    default:
                        LogMsg(CommLogEventArgs.LogCategory.Error, $"Invalid address: {msg.Address}");
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

            // Listen again.
            _udpClient.BeginReceive(new AsyncCallback(OscReceive), this);
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
