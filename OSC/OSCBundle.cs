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
    /// An OSC Bundle consists of the OSC-string "#bundle" followed by an OSC Time Tag, followed by zero or more
    /// OSC Message or Bundle Elements. An OSC Bundle Element consists of its TODOX size and its contents. The size
    /// is an int32 representing the number of 8-bit bytes in the contents, and will always be a multiple of 4.
    /// The contents are either an OSC Message or an OSC Bundle.
    /// Note this recursive definition: bundle may contain bundles.
    /// </summary>
    public class Bundle
    {
        //TODOX support nested bundles? public List<Bundle> Bundles { get; private set; } = new List<Bundle>(); other apps flatten them out.

        #region Constants
        /// <summary>Bundle marker</summary>
        public const string BUNDLE_ID = "#bundle";
        #endregion

        #region Properties
        /// <summary>The timetag.</summary>
        public TimeTag TimeTag { get; private set; } = new TimeTag();

        /// <summary>Contained messages.</summary>
        public List<Message> Messages { get; private set; } = new List<Message>();
        #endregion

        #region Public functions
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="ttag"></param>
        public Bundle(TimeTag ttag)
        {
            TimeTag = ttag;
        }

        /// <summary>
        /// Format to binary form.
        /// </summary>
        /// <returns></returns>
        public List<byte> Pack()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(OSCUtils.Pack(BUNDLE_ID));
            bytes.AddRange(OSCUtils.Pack(TimeTag.Raw));
            //Bundles.ForEach(b => bytes.AddRange(b.Pack()));
            Messages.ForEach(m => bytes.AddRange(m.Pack()));
            bytes.Pad();

            return bytes;
        }

        /// <summary>
        /// Factory function.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static Bundle Unpack(byte[] bytes)
        {
            int index = 0;
            bool ok = true;

            // Parse marker.
            string marker = null;
            if (ok)
            {
                ok = OSCUtils.Unpack(bytes, ref index, ref marker);
            }
            if (ok)
            {
                ok = marker == BUNDLE_ID;
            }
            if (!ok)
            {
                //TODOX   Error("Invalid marker string");
            }

            // Parse timetag.
            ulong tt = 0;
            if (ok)
            {
                ok = OSCUtils.Unpack(bytes, ref index, ref tt);
            }

            // Parse bundles and messages.
            List<Bundle> bundles = new List<Bundle>();
            List<Message> messages = new List<Message>();

            if (ok)
            {
                while (index < bytes.Count() && ok)
                {
                    if (bytes[index] == '#') // bundle?
                    {
                        Bundle b = Bundle.Unpack(bytes);
                        if (b != null)
                        {
                            bundles.Add(b);
                        }
                        else
                        {
                            ok = false;
                            // TODOX error message
                        }
                    }
                    else // message?
                    {
                        Message m = Message.Unpack(bytes);
                        if (m != null)
                        {
                            messages.Add(m);
                        }
                        else
                        {
                            ok = false;
                            // TODOX error message
                        }
                    }
                }
            }

            return ok ? new Bundle(new TimeTag(tt)) {/* Bundles = bundles, */Messages = messages } : null;
        }
        #endregion
    }
}