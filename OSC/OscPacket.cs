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
    public class Packet
    {
        #region Typed converters to binary
        public List<byte> Pack(string value)
        {
            List<byte> bytes = new List<byte>();
            value.ToList().ForEach(v => bytes.Add((byte)v));
            bytes.Add(0); // terminate
            bytes.Pad(); // pad to 4x bytes
            return bytes;
        }

        public List<byte> Pack(int value)
        {
            List<byte> bytes = new List<byte>(BitConverter.GetBytes(value));
            bytes.FixEndian();
            return bytes;
        }

        public List<byte> Pack(ulong value)
        {
            List<byte> bytes = new List<byte>(BitConverter.GetBytes(value));
            bytes.FixEndian();
            return bytes;
        }

        public List<byte> Pack(float value)
        {
            List<byte> bytes = new List<byte>(BitConverter.GetBytes(value));
            bytes.FixEndian();
            return bytes;
        }

        public List<byte> Pack(List<byte> value)
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(Pack(value.Count));
            bytes.AddRange(value);
            bytes.Pad(); // pad to 4x bytes
            return bytes;
        }
        #endregion

        #region Typed converters from binary
        public bool Unpack(byte[] msg, ref int start, ref string val)
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
                        if(OscCommon.IsReadable(msg[index]))
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
                        if (OscCommon.IsReadable(msg[index]))
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

        public bool Unpack(byte[] msg, ref int start, ref int val)
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

        public bool Unpack(byte[] msg, ref int start, ref ulong val)
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

        public bool Unpack(byte[] msg, ref int start, ref float val)
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

        public bool Unpack(byte[] msg, ref int start, ref List<byte> val)
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
    }
}