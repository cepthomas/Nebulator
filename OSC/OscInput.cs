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
        public int ClientPort { get; set; } = -1;

        /// <inheritdoc />
        public CommCaps Caps { get; private set; } = OscUtils.InitCaps();

        /// <inheritdoc />
        public bool Inited { get; private set; } = false;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public OscInput()
        {
        }

        /// <inheritdoc />
        public bool Construct(string name)
        {
            CommName = name;
            return true;
        }

        /////// ??
        //Socket.ReceiveAsync(SocketAsyncEventArgs)


        static void OnUdpData(IAsyncResult result)
        {
            // this is what had been passed into BeginReceive as the second parameter:
            UdpClient udpClient = result.AsyncState as UdpClient;

            // points towards whoever had sent the message:
            IPEndPoint source = new IPEndPoint(0, 0);

            // get the actual message and fill out the source:
            byte[] message = udpClient.EndReceive(result, ref source);

            //string ip = source.Address.ToString();
            //server cs = new server();
            //cs.updateData(message, ip);

            // start listening again
            udpClient.BeginReceive(new AsyncCallback(OnUdpData), udpClient);
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

                /*

                _udpClient.BeginReceive(new AsyncCallback(OnUdpData), _udpClient);

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

                */

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
//           _udpClient?.Start();
        }

        /// <inheritdoc />
        public void Stop()
        {
//           _udpClient?.Stop();
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



    /*


    /////////////////////////////////
    public delegate void HandleOscPacket(OscPacket packet);
    public delegate void HandleBytePacket(byte[] packet);

    public class UDPListener : IDisposable
    {
        public int Port { get; private set; }
        
        object callbackLock;

        UdpClient receivingUdpClient;
        IPEndPoint RemoteIpEndPoint;

        HandleBytePacket BytePacketCallback = null;
        HandleOscPacket OscPacketCallback = null;

        Queue<byte[]> queue;
        ManualResetEvent ClosingEvent;

        public UDPListener(int port)
        {
            Port = port;
            queue = new Queue<byte[]>();
            ClosingEvent = new ManualResetEvent(false);
            callbackLock = new object();

            // try to open the port 10 times, else fail
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    receivingUdpClient = new UdpClient(port);
                    break;
                }
                catch (Exception)
                {
                    // Failed in ten tries, throw the exception and give up
                    if (i >= 9)
                        throw;

                    Thread.Sleep(5);
                }
            }
            RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

            // setup first async event
            AsyncCallback callBack = new AsyncCallback(ReceiveCallback);
            receivingUdpClient.BeginReceive(callBack, null);
        }

        public UDPListener(int port, HandleOscPacket callback) : this(port)
        {
            this.OscPacketCallback = callback;
        }

        public UDPListener(int port, HandleBytePacket callback) : this(port)
        {
            this.BytePacketCallback = callback;
        }

        void ReceiveCallback(IAsyncResult result)
        {
            Monitor.Enter(callbackLock);
            Byte[] bytes = null;

            try
            {
                bytes = receivingUdpClient.EndReceive(result, ref RemoteIpEndPoint);
            }
            catch (ObjectDisposedException e)
            { 
                // Ignore if disposed. This happens when closing the listener
            }

            // Process bytes
            if (bytes != null && bytes.Length > 0)
            {
                if (BytePacketCallback != null)
                {
                    BytePacketCallback(bytes);
                }
                else if (OscPacketCallback != null)
                {
                    OscPacket packet = null;
                    try
                    {
                        packet = OscPacket.GetPacket(bytes);
                    }
                    catch (Exception e)
                    {
                        // If there is an error reading the packet, null is sent to the callback
                    }

                    OscPacketCallback(packet);
                }
                else
                {
                    lock (queue)
                    {
                        queue.Enqueue(bytes);
                    }
                }
            }

            if (closing)
                ClosingEvent.Set();
            else
            {
                // Setup next async event
                AsyncCallback callBack = new AsyncCallback(ReceiveCallback);
                receivingUdpClient.BeginReceive(callBack, null);
            }
            Monitor.Exit(callbackLock);
        }

        bool closing = false;
        public void Close()
        {
            lock (callbackLock)
            {
                ClosingEvent.Reset();
                closing = true;
                receivingUdpClient.Close();
            }
            ClosingEvent.WaitOne();
            
        }

        public void Dispose()
        {
            this.Close();
        }

        public OscPacket Receive()
        {
            if (closing) throw new Exception("UDPListener has been closed.");

            lock (queue)
            {
                if (queue.Count() > 0)
                {
                    byte[] bytes = queue.Dequeue();
                    var packet = OscPacket.GetPacket(bytes);
                    return packet;
                }
                else
                    return null;
            }
        }

        public byte[] ReceiveBytes()
        {
            if (closing) throw new Exception("UDPListener has been closed.");

            lock (queue)
            {
                if (queue.Count() > 0)
                {
                    byte[] bytes = queue.Dequeue();
                    return bytes;
                }
                else
                    return null;
            }
        }
        
    }
    */
}
