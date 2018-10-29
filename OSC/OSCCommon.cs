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
    public static class OSCUtils
    {
        public static CommCaps InitCaps()
        {
            CommCaps caps = new CommCaps() //TODOX
            {

            };

            return caps;
        }

        public static List<byte> Format(string value)
        {
            List<byte> bytes = new List<byte>();
            value.ToList().ForEach(v => bytes.Add((byte)v));
            bytes.Add(0); // terminate
            bytes.Pad(); // pad to 4x bytes
            return bytes;
        }

        public static List<byte> Format(int value)
        {
            List<byte> bytes = new List<byte>(BitConverter.GetBytes(value));
            bytes.FixEndian();
            return bytes;
        }

        public static List<byte> Format(ulong value)
        {
            List<byte> bytes = new List<byte>(BitConverter.GetBytes(value));
            bytes.FixEndian();
            return bytes;
        }

        public static List<byte> Format(float value)
        {
            List<byte> bytes = new List<byte>(BitConverter.GetBytes(value));
            bytes.FixEndian();
            return bytes;
        }

        public static List<byte> Format(List<byte> value)
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(Format(value.Count));
            bytes.AddRange(value);
            bytes.Pad(); // pad to 4x bytes
            return bytes;
        }

        public static bool Parse(byte[] msg, ref int start, ref string val)
        {
            bool ok = false;
            bool isTerm = false;
            int nextStart = start;

            for(int i = start; i < msg.Length && nextStart == start; i++)
            {
                if(isTerm)
                {
                    if(msg[i] != 0)
                    {
                        // transition term to non-term == end of string
                        nextStart = i;
                    }
                }
                else
                {
                    isTerm = msg[i] == 0;
                }
            }

            // Check for valid form.
            if(nextStart > start && (nextStart - start) % 4 == 0)
            {
                // All good, clean up.
                val = Encoding.ASCII.GetString(msg, start, nextStart - start).Replace("\0", "");
                start = nextStart;
                ok = true;
            }
            else // ng
            {
                val = "";
                start = -1;
            }

            return ok;
        }

        public static bool Parse(byte[] msg, ref int start, ref int val)
        {
            bool ok = false;
            int nextStart = start;

            // Sanity checks.
            if (msg.Length - start >= 4)
            {
                var ss = msg.Subset(start, 4).ToList();
                ss.FixEndian();

                val = BitConverter.ToInt32(msg, start);
                start += 4;
                ok = true;
            }

            return ok;
        }

        public static bool Parse(byte[] msg, ref int start, ref ulong val)
        {
            bool ok = false;
            int nextStart = start;

            // Sanity checks.
            if (msg.Length - start >= 8)
            {
                var ss = msg.Subset(start, 8).ToList();
                ss.FixEndian();

                val = BitConverter.ToUInt64(msg, start);
                start += 4;
                ok = true;
            }

            return ok;
        }

        public static bool Parse(byte[] msg, ref int start, ref float val)
        {
            bool ok = false;
            int nextStart = start;

            // Sanity checks.
            if (msg.Length - start >= 4)
            {
                var ss = msg.Subset(start, 4).ToList();
                ss.FixEndian();

                val = BitConverter.ToSingle(msg, start);
                start += 4;
                ok = true;
            }

            return ok;
        }

        public static bool Parse(byte[] msg, ref int start, ref List<byte> val)
        {
            bool ok = false;
            int nextStart = start;

            // Sanity checks.
            if (msg.Length - start >= 4)
            {
                int blen = 0;
                ok = Parse(msg, ref start, ref blen);
                if(ok)
                {
                    ok = blen <= (val.Count - 4);
                }

                if (ok)
                {
                    val.AddRange(msg.Subset(4, blen));
                }
            }

            return ok;
        }

        // extension
        public static void Pad(this List<byte> bytes)
        {
            for (int i = 0; i < bytes.Count % 4; i++)
            {
                bytes.Add(0);
            }
        }

        // extension
        internal static void FixEndian(this List<byte> bytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                bytes.Reverse();
            }
        }
    }


}