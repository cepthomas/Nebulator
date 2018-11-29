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
    /// Abstraction layer between OSC comm and Nebulator steps. aka OSC client.
    /// </summary>
    public class OscOutput : NOutput
    {
        #region Fields
        /// <summary>OSC output device.</summary>
        UdpClient _udpClient;

        /// <summary>Access synchronizer.</summary>
        object _oscLock = new object();

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

        /// <summary>Where to?</summary>
        public string IP { get; private set; } = Utils.UNKNOWN_STRING;

        /// <summary>Where to?</summary>
        public int Port { get; private set; } = -1;

        /// <inheritdoc />
        public CommCaps Caps { get; private set; } = OscUtils.InitCaps();
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public OscOutput()
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
                if(parts.Count == 3 && parts[0] == "OSC")
                {
                    if (int.TryParse(parts[2], out int port))
                    {
                        IP = parts[1];
                        Port = port;
                        _udpClient = new UdpClient(IP, Port);
                        inited = true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMsg(CommLogEventArgs.LogCategory.Error, $"Init OSC out failed: {ex.Message}");
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
            lock (_oscLock)
            {
                if (_udpClient != null)
                {
                    List<int> msgs = new List<int>();
                    Message msg = null;

                    switch (step)
                    {
                        case StepNoteOn non:
                            // /note/ channel notenum vel
                            msg = new Message("/note");
                            msg.Data.Add(non.ChannelNumber);
                            msg.Data.Add(non.NoteNumber);
                            msg.Data.Add(non.VelocityToPlay);

                            if (non.Duration.TotalTocks > 0) // specific duration
                            {
                                // Remove any lingering note offs and add a fresh one.
                                _stops.RemoveAll(s => s.NoteNumber == non.NoteNumber && s.ChannelNumber == non.ChannelNumber);

                                _stops.Add(new StepNoteOff()
                                {
                                    Comm = non.Comm,
                                    ChannelNumber = non.ChannelNumber,
                                    NoteNumber = Utils.Constrain(non.NoteNumber, Caps.MinNote, Caps.MaxNote),
                                    Expiry = non.Duration.TotalTocks
                                });
                            }
                            break;

                        case StepNoteOff noff:
                            // /note/ channel notenum 0
                            msg = new Message("/note");
                            msg.Data.Add(noff.ChannelNumber);
                            msg.Data.Add(noff.NoteNumber);
                            msg.Data.Add(0);

                            break;

                        case StepControllerChange ctl:
                            // /controller/ channel ctlnum val
                            msg = new Message("/controller");
                            msg.Data.Add(ctl.ChannelNumber);
                            msg.Data.Add(ctl.ControllerId);
                            msg.Data.Add(ctl.Value);
                            break;

                        case StepPatch stt:
                            // ignore n/a
                            break;

                        default:
                            break;
                    }

                    if (msg != null)
                    {
                        List<byte> bytes = msg.Pack();
                        if (bytes != null)
                        {
                            if (msg.Errors.Count == 0)
                            {
                                _udpClient.Send(bytes.ToArray(), bytes.Count);
                                LogMsg(CommLogEventArgs.LogCategory.Send, step.ToString());
                            }
                            else
                            {
                                msg.Errors.ForEach(e => LogMsg(CommLogEventArgs.LogCategory.Error, e));
                            }
                        }
                        else
                        {
                            LogMsg(CommLogEventArgs.LogCategory.Error, step.ToString());
                        }
                    }
                    else
                    {
                        LogMsg(CommLogEventArgs.LogCategory.Error, step.ToString());
                    }
                }
            }

            return ret;
        }

        /// <inheritdoc />
        public void Kill(int? channel)
        {
            if (channel is null)
            {
                // TODOX all?
            }
            else
            {
                StepControllerChange step = new StepControllerChange()
                {
                    ChannelNumber = (int)channel,
                    ControllerId = 123, //(int)MidiController.AllNotesOff,
                    Value = 0
                };

                Send(step);
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
