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
    /// An OSC Bundle consists of the OSC-string "#bundle" followed by an OSC Time Tag, followed by zero or more
    /// OSC Message or Bundle Elements. An OSC Bundle Element consists of its size and its contents. The size
    /// is an int32 representing the number of 8-bit bytes in the contents, and will always be a multiple of 4.
    /// The contents are either an OSC Message or an OSC Bundle.
    /// Note this recursive definition: bundle may contain bundles. Future? other apps flatten them out.
    /// </summary>
    public class Bundle : Packet
    {
        #region Constants
        /// <summary>Bundle marker</summary>
        public const string BUNDLE_ID = "#bundle";
        #endregion

        #region Properties
        /// <summary>The timetag.</summary>
        public TimeTag TimeTag { get; set; } = new TimeTag();

        /// <summary>Contained messages.</summary>
        public List<Message> Messages { get; private set; } = new List<Message>();

        /// <summary>Parse errors.</summary>
        public List<string> Errors { get; private set; } = new List<string>();
        #endregion

        #region Public functions
        /// <summary>
        /// Format to binary form.
        /// </summary>
        /// <returns></returns>
        public List<byte> Pack()
        {
            List<byte> bytes = new List<byte>();

            // Front matter
            bytes.AddRange(Pack(BUNDLE_ID));
            bytes.AddRange(Pack(TimeTag.Raw));

            // Messages
            foreach(Message m in Messages)
            {
                List<byte> mb = m.Pack();
                bytes.AddRange(Pack(mb.Count));
                bytes.AddRange(mb);
            }

            // Tail
            bytes.Pad();
            bytes.InsertRange(0, Pack(bytes.Count));
            return bytes;
        }

        /// <summary>
        /// Factory function.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public bool Unpack(byte[] bytes)
        {
            int index = 0;
            bool ok = true;
            Errors.Clear();
            Messages.Clear();

            // Parse marker.
            string marker = null;
            if (ok)
            {
                ok = Unpack(bytes, ref index, ref marker);
            }
            if (ok)
            {
                ok = marker == BUNDLE_ID;
            }
            if (!ok)
            {
                Errors.Add("Invalid marker string");
            }

            // Parse timetag.
            ulong tt = 0;
            if (ok)
            {
                ok = Unpack(bytes, ref index, ref tt);
            }
            if (ok)
            {
                TimeTag = new TimeTag(tt);
            }

            // Parse bundles and messages.
            List<Bundle> bundles = new List<Bundle>();

            if (ok)
            {
                while (index < bytes.Count() && ok)
                {
                    if (bytes[index] == '#') // bundle?
                    {
                        Bundle b = new Bundle();
                        if (b.Unpack(bytes))
                        {
                            bundles.Add(b);
                        }
                        else
                        {
                            ok = false;
                            Errors.Add("Couldn't unpack the bundle");
                        }
                    }
                    else // message?
                    {
                        Message m = new Message();
                        if(m.Unpack(bytes))
                        {
                            Messages.Add(m);
                        }
                        else
                        {
                            ok = false;
                            Errors.Add("Couldn't unpack the message");
                        }
                    }
                }
            }

            return ok;
        }
        #endregion
    }
}