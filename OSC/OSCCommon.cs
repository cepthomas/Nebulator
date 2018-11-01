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
        #region Misc
        public static CommCaps InitCaps()
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

        #region Typed converters to binary
        public static List<byte> Pack(string value)
        {
            List<byte> bytes = new List<byte>();
            value.ToList().ForEach(v => bytes.Add((byte)v));
            bytes.Add(0); // terminate
            bytes.Pad(); // pad to 4x bytes
            return bytes;
        }

        public static List<byte> Pack(int value)
        {
            List<byte> bytes = new List<byte>(BitConverter.GetBytes(value));
            bytes.FixEndian();
            return bytes;
        }

        public static List<byte> Pack(ulong value)
        {
            List<byte> bytes = new List<byte>(BitConverter.GetBytes(value));
            bytes.FixEndian();
            return bytes;
        }

        public static List<byte> Pack(float value)
        {
            List<byte> bytes = new List<byte>(BitConverter.GetBytes(value));
            bytes.FixEndian();
            return bytes;
        }

        public static List<byte> Pack(List<byte> value)
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(Pack(value.Count));
            bytes.AddRange(value);
            bytes.Pad(); // pad to 4x bytes
            return bytes;
        }
        #endregion

        #region Typed converters from binary
        public static bool Unpack(byte[] msg, ref int start, ref string val)
        {
            bool ok = true;

            // 0=start 1=collecting-chars 2=looking-for-end 3=done
            int state = 0;
            int index = start;
            StringBuilder sb = new StringBuilder();

            while(ok && state <= 2)
            {
                switch (state)
                {
                    case 0:
                        if(IsReadable(msg[index]))
                        {
                            sb.Append((char)msg[index]);
                            index++;
                            state = 1;
                        }
                        else // ng value at beginning
                        {
                            ok = false;
                        }
                        break;

                    case 1:
                        if (IsReadable(msg[index]))
                        {
                            sb.Append((char)msg[index]);
                            index++;
                        }
                        else if(msg[index] == 0) // at end part
                        {
                            state = 2;
                            index++;
                        }
                        else // junk char
                        {
                            ok = false;
                        }
                        break;

                    case 2:
                        if ((index - start) % 4 == 0) // done?
                        {
                            state = 3;
                        }
                        else // bump
                        {
                            if (msg[index] == 0) // bump
                            {
                                index++;
                            }
                            else // unexpected char in term area
                            {
                                ok = false;
                            }
                        }
                        break;
                }
            }

            // What happened?
            if(ok)
            {
                val = sb.ToString();
                start = index;
            }
            else
            {
                val = "";
                start = -1;
            }

            return ok;
        }

        public static bool Unpack(byte[] msg, ref int start, ref int val)
        {
            bool ok = false;
            int nextStart = start;

            // Sanity checks.
            if (msg.Length - start >= 4)
            {
                var ss = msg.Subset(start, 4).ToList(); //TODOX not very efficient/smart...
                ss.FixEndian();

                val = BitConverter.ToInt32(ss.ToArray(), 0);
                start += 4;
                ok = true;
            }

            return ok;
        }

        public static bool Unpack(byte[] msg, ref int start, ref ulong val)
        {
            bool ok = false;
            int nextStart = start;

            // Sanity checks.
            if (msg.Length - start >= 8)
            {
                var ss = msg.Subset(start, 8).ToList();
                ss.FixEndian();

                val = BitConverter.ToUInt64(ss.ToArray(), 0);
                start += 4;
                ok = true;
            }

            return ok;
        }

        public static bool Unpack(byte[] msg, ref int start, ref float val)
        {
            bool ok = false;
            int nextStart = start;

            // Sanity checks.
            if (msg.Length - start >= 4)
            {
                var ss = msg.Subset(start, 4).ToList();
                ss.FixEndian();

                val = BitConverter.ToSingle(ss.ToArray(), 0);
                start += 4;
                ok = true;
            }

            return ok;
        }

        public static bool Unpack(byte[] msg, ref int start, ref List<byte> val)
        {
            bool ok = false;
            //int nextStart = start;
            int blen = 0;

            if (msg.Length - start >= 4)
            {
                ok = Unpack(msg, ref start, ref blen);
                if(ok)
                {
                    ok = blen <= (msg.Length - start);
                }
            }

            if (ok)
            {
                val.AddRange(msg.Subset(4, blen));
            }

            // Remove pad.
            while(start % 4 != 0)
            {
                start++;
            }

            return ok;
        }
        #endregion


        public static bool IsReadable(byte b)
        {
            return b >= 32 && b <= 126;
        }


        #region Extensions TODOX in common?
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
        #endregion
    }
}