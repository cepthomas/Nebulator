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
                if (int.TryParse(name, out int port))
                {
                    Port = port;
                    inited = true;
                }

                if(inited)
                {
                    Start();
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
            _udpClient.BeginReceive(new AsyncCallback(OscReceive), _udpClient);
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
        /// <param name="result"></param>
        static void OscReceive(IAsyncResult result)
        {
            UdpClient udpClient = result.AsyncState as UdpClient;
            IPEndPoint sender = new IPEndPoint(0, 0);
            // Get input. TODOY handle it - CommInputEvent - payload is a Step?
            byte[] message = udpClient.EndReceive(result, ref sender);
            // Listen again.
            udpClient.BeginReceive(new AsyncCallback(OscReceive), udpClient);
        }

        //public Bundle Receive()
        //{
        //    Bundle b = null;
        //    IPEndPoint ip = null;
        //    byte[] bytes = _udpClient.Receive(ref ip);
        //    if (bytes != null && bytes.Length > 0)
        //    {
        //        // unpack - check for bundle or message TODOX nested bundles?
        //        if (bytes[0] == '#')
        //        {
        //            Bundle bnest = Bundle.Unpack(bytes);
        //        }
        //        else
        //        {
        //            Message m = Message.Unpack(bytes);
        //        }
        //        //    // Decode the message. We only care about a few. TODOX now what? pass back to the script?
        //        // Midi does like this:
        //        // MY_MIDI_IN = createInput("MPK mini");
        //        // createController(MY_MIDI_IN, 1, 1, MOD1); // modulate eq
        //        // createController(MY_MIDI_IN, 1, 2, CTL2); // since I don't have a pitch knob, I'll use this instead
        //        // createController(MY_MIDI_IN, 1, 3, CTL3); // another controller
        //        // createController(MY_MIDI_IN, 0, 4, BACK_COLOR); // change ui color
        //        // createController(MY_MIDI_IN, 1, NoteControl, KBD_NOTE);
        //        //    if (step != null)
        //        //    {
        //        //        // Pass it up for handling.
        //        //        CommInputEventArgs args = new CommInputEventArgs() { Step = step };
        //        //        CommInputEvent?.Invoke(this, args);
        //        //        LogMsg(CommLogEventArgs.LogCategory.Recv, step.ToString());
        //        //    }
        //    }
        //    return b;
        //}

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
