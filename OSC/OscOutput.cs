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
using Nebulator.Device;


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
        object _lock = new object();

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

        /// <summary>Where to?</summary>
        public string IP { get; private set; } = Utils.UNKNOWN_STRING;

        /// <summary>Where to?</summary>
        public int Port { get; private set; } = -1;
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

            DeviceName = "Invalid"; // default

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
                if(parts.Count == 3)
                {
                    if (int.TryParse(parts[2], out int port))
                    {
                        IP = parts[1];
                        Port = port;
                        _udpClient = new UdpClient(IP, Port);
                        inited = true;
                        DeviceName = $"{IP}:{Port}";
                    }
                }
            }
            catch (Exception ex)
            {
                LogMsg(DeviceLogCategory.Error, $"Init OSC out failed: {ex.Message}");
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
            lock (_lock)
            {
                if (_udpClient != null)
                {
                    List<int> msgs = new List<int>();
                    Message msg = null;

                    switch (step)
                    {
                        case StepNoteOn non:
                            // /noteon/ channel notenum vel
                            msg = new Message() { Address = "/noteon" };
                            msg.Data.Add(non.ChannelNumber);
                            msg.Data.Add(non.NoteNumber);
                            msg.Data.Add(non.VelocityToPlay);

                            if (non.Duration.TotalTocks > 0) // specific duration
                            {
                                // Remove any lingering note offs and add a fresh one.
                                _stops.RemoveAll(s => s.NoteNumber == non.NoteNumber && s.ChannelNumber == non.ChannelNumber);

                                _stops.Add(new StepNoteOff()
                                {
                                    Device = non.Device,
                                    ChannelNumber = non.ChannelNumber,
                                    NoteNumber = Utils.Constrain(non.NoteNumber, 0, OscCommon.MAX_NOTE),
                                    Expiry = non.Duration.TotalTocks
                                });
                            }
                            break;

                        case StepNoteOff noff:
                            // /noteoff/ channel notenum
                            msg = new Message() { Address = "/noteoff" };
                            msg.Data.Add(noff.ChannelNumber);
                            msg.Data.Add(noff.NoteNumber);

                            break;

                        case StepControllerChange ctl:
                            // /controller/ channel ctlnum val
                            msg = new Message() { Address = "/controller" };
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
                                LogMsg(DeviceLogCategory.Send, step.ToString());
                            }
                            else
                            {
                                msg.Errors.ForEach(e => LogMsg(DeviceLogCategory.Error, e));
                            }
                        }
                        else
                        {
                            LogMsg(DeviceLogCategory.Error, step.ToString());
                        }
                    }
                    else
                    {
                        LogMsg(DeviceLogCategory.Error, step.ToString());
                    }
                }
            }

            return ret;
        }

        /// <inheritdoc />
        public void Kill(int? channel)
        {
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
