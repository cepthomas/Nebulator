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
                   _udpClient = new MidiIn(ind);
                   _udpClient.MessageReceived += MidiIn_MessageReceived;
                   _udpClient.ErrorReceived += MidiIn_ErrorReceived;
                   _udpClient.Start();
                   Inited = true;
                }

                _udpClient = new UdpClient(ClientPort);
                udpClient.MessageReceived += MidiIn_MessageReceived;
                udpClient.ErrorReceived += MidiIn_ErrorReceived;
                _udpClient.Start();
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
           _udpClient?.Start();
        }

        /// <inheritdoc />
        public void Stop()
        {
           _udpClient?.Stop();
        }

        /// <inheritdoc />
        public void Housekeep()
        {
        }
        #endregion

        #region Private functions
        public Bundle Receive() // needs a thread/async TODOX 
        {
            Bundle b = null;

            IPEndPoint ip = null;
            byte[] bytes = _udpClient.Receive(ref ip);

            if (bytes != null && bytes.Length > 0)
            {
                // unpack - check for bundle or message TODOX nested bundles?
                if (bytes[0] == '#')
                {
                    Bundle bnest = Bundle.Unpack(bytes);
                }
                else
                {
                    Message m = Message.Unpack(bytes);
                }


                //    // Decode the message. We only care about a few. TODOX now what? pass back to the script?
                // Midi does like this:
                // MY_MIDI_IN = createInput("MPK mini");
                // createController(MY_MIDI_IN, 1, 1, MOD1); // modulate eq
                // createController(MY_MIDI_IN, 1, 2, CTL2); // since I don't have a pitch knob, I'll use this instead
                // createController(MY_MIDI_IN, 1, 3, CTL3); // another controller
                // createController(MY_MIDI_IN, 0, 4, BACK_COLOR); // change ui color
                // createController(MY_MIDI_IN, 1, NoteControl, KBD_NOTE);


                //    if (step != null)
                //    {
                //        // Pass it up for handling.
                //        CommInputEventArgs args = new CommInputEventArgs() { Step = step };
                //        CommInputEvent?.Invoke(this, args);
                //        LogMsg(CommLogEventArgs.LogCategory.Recv, step.ToString());
                //    }

            }

            return b;
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
