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
    /// Bunch of utilities for formatting and parsing. Not documented because they are self-explanatory.
    /// </summary>
    public static class OscCommon
    {
        #region Misc
        public static CommCaps InitCaps() // TODOX update these
        {
            return new CommCaps()
            {
                NumChannels = 16,
                MinVolume = 0,
                MaxVolume = 127,
                MinNote = 0,
                MaxNote = 127,
                MinControllerValue = 0,
                MaxControllerValue = 127,
                MinPitchValue = 0,
                MaxPitchValue = 16383
            };
        }
        #endregion

        #region Utilities
        /// <summary>
        /// Add 0s to make multiple of 4.
        /// </summary>
        /// <param name="bytes"></param>
        public static void Pad(this List<byte> bytes)
        {
            for (int i = 0; i < bytes.Count % 4; i++)
            {
                bytes.Add(0);
            }
        }

        /// <summary>
        /// Handle endianness.
        /// </summary>
        /// <param name="bytes">Data in place.</param>
        public static void FixEndian(this List<byte> bytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                bytes.Reverse();
            }
        }

        /// <summary>
        /// Make readable string.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="delim"></param>
        /// <returns></returns>
        public static string Dump(this List<byte> bytes, string delim = "")
        {
            StringBuilder sb = new StringBuilder();
            bytes.ForEach(b => { if (IsReadable(b)) sb.Append((char)b); else sb.AppendFormat(@"{0}{1:000}", delim, b); });
            return sb.ToString();
        }

        public static bool IsReadable(byte b)
        {
            return b >= 32 && b <= 126;
        }
        #endregion

    }
}