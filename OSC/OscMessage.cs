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
    /// Message contains an address, a comma followed by one or more data type identifiers,
    /// then the data itself follows in binary encoding.
    /// 
    /// Data types supported are just the basics from the OSC Specs 1.0:
    /// i = int32 = 32-bit big-endian two's complement integer
    /// f = float32 = 32-bit big-endian IEEE 754 floating point number
    /// s = OSC-string = A sequence of non-null ASCII characters followed by a null, followed by 0-3 additional
    ///     null characters to make the total number of bits a multiple of 32.
    /// b = OSC-blob = An int32 size count, followed by that many 8-bit bytes of arbitrary binary data, followed by 0-3 
    ///     additional zero bytes to make the total number of bits a multiple of 32.
    /// </summary>
    public class Message : Packet
    {
        #region Properties
        /// <summary>Storage of address.</summary>
        public string Address { get; set; } = null;

        /// <summary>Data elements in the message.</summary>
        public List<object> Data { get; private set; } = new List<object>();

        /// <summary>Parse errors.</summary>
        public List<string> Errors { get; private set; } = new List<string>();
        #endregion

        #region Public functions
        /// <summary>
        /// Client request to format the message.
        /// </summary>
        /// <returns></returns>
        public List<byte> Pack()
        {
            bool ok = true;

            // Data type string.
            StringBuilder dtype = new StringBuilder();

            // Data values.
            List<byte> dvals = new List<byte>();

            Data.ForEach(d =>
            {
                switch (d)
                {
                    case int i:
                        dtype.Append('i');
                        dvals.AddRange(Pack(i));
                        break;

                    case float f:
                        dtype.Append('f');
                        dvals.AddRange(Pack(f));
                        break;

                    case string s:
                        dtype.Append('s');
                        dvals.AddRange(Pack(s));
                        break;

                    case List<byte> b:
                        dtype.Append('b');
                        dvals.AddRange(Pack(b));
                        break;

                    default:
                        Errors.Add($"Unknown type: {d.GetType()}");
                        ok = false;
                        break;
                }
            });

            if (ok)
            {
                // Put it all together.
                List<byte> bytes = new List<byte>();
                bytes.AddRange(Pack(Address));
                dtype.Insert(0, ',');
                bytes.AddRange(Pack(dtype.ToString()));
                bytes.AddRange(dvals);
                return bytes;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Factory parser function.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public bool Unpack(byte[] bytes)
        {
            int index = 0;
            bool ok = true;
            Data.Clear();

            // Parse address.
            string address = null;
            if (ok)
            {
                ok = Unpack(bytes, ref index, ref address);
            }
            if (ok)
            {
                Address = address;
            }
            else
            {
                Errors.Add("Invalid address string");
            }

            // Parse data types.
            string dtypes = null;
            if (ok)
            {
                ok = Unpack(bytes, ref index, ref dtypes);
            }
            if (ok)
            {
                ok = (dtypes.Length >= 1) && (dtypes[0] == ',');
            }

            // Parse data values.
            if (ok)
            {
                for (int i = 1; i < dtypes.Length && ok; i++)
                {
                    switch (dtypes[i])
                    {
                        case 'i':
                            int di = 0;
                            ok = Unpack(bytes, ref index, ref di);
                            if (ok)
                            {
                                Data.Add(di);
                            }
                            break;

                        case 'f':
                            float df = 0;
                            ok = Unpack(bytes, ref index, ref df);
                            if (ok)
                            {
                                Data.Add(df);
                            }
                            break;

                        case 's':
                            string ds = "";
                            ok = Unpack(bytes, ref index, ref ds);
                            if (ok)
                            {
                                Data.Add(ds);
                            }
                            break;

                        case 'b':
                            List<byte> db = new List<byte>();
                            ok = Unpack(bytes, ref index, ref db);
                            if (ok)
                            {
                                Data.Add(db);
                            }
                            break;

                        default:
                            ok = false;
                            Errors.Add($"Invalid data type: {dtypes[i]}");
                            break;
                    }
                }
            }

            return ok;
        }

        /// <summary>
        /// Readable.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"Address:{Address} Data:");

            Data.ForEach(o => sb.Append(o.ToString() + " "));

            return sb.ToString();
        }
        #endregion
    }
}