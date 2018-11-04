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
    /// Data types - OSC Specs 1.0:
    /// i = int32 = 32-bit big-endian two's complement integer
    /// f = float32 = 32-bit big-endian IEEE 754 floating point number
    /// s = OSC-string = A sequence of non-null ASCII characters followed by a null, followed by 0-3 additional null characters
    ///     to make the total number of bits a multiple of 32.
    /// b = OSC-blob = An int32 size count, followed by that many 8-bit bytes of arbitrary binary data, followed by 0-3 
    ///     additional zero bytes to make the total number of bits a multiple of 32.
    ///
    /// Some OSC applications communicate among instances of themselves with additional, nonstandard argument types beyond
    /// those specified above. OSC applications are not required to recognize these types; an OSC application should 
    /// discard any message whose OSC Type Tag String contains any unrecognized OSC Type Tags. An application that does 
    /// use any additional argument types must encode them with the OSC Type Tags in this table:
    /// h = 64 bit big-endian two's complement integer
    /// t = OSC-timetag
    /// d = 64 bit ("double") IEEE 754 floating point number
    /// S = Alternate type represented as an OSC-string (for example, for systems that differentiate "symbols" from "strings")
    /// c = an ascii character, sent as 32 bits
    /// r = 32 bit RGBA color
    /// m = 4 byte MIDI message. Bytes from MSB to LSB are: port id, status byte, data1, data2
    /// T = True. No bytes are allocated in the argument data.
    /// F = False. No bytes are allocated in the argument data.
    /// N = Nil. No bytes are allocated in the argument data.
    /// I = Infinitum. No bytes are allocated in the argument data.
    /// [ = Indicates the beginning of an array. The tags following are for data in the Array until a close brace tag is reached.
    /// ] = Indicates the end of an array.
    /// </summary>
    public class Message
    {
        #region Properties
        /// <summary>Storage of address.</summary>
        public string Address { get; private set; } = null;

        /// <summary>Data elements in the message.</summary>
        public List<object> Data { get; private set; } = new List<object>();

        /// <summary>Parse errors.</summary>
        public List<string> Errors { get; private set; } = new List<string>();
        #endregion

        #region Public functions
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="address"></param>
        public Message(string address)
        {
            Address = address;
        }

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
                        dvals.AddRange(OscUtils.Pack(i));
                        break;

                    case float f:
                        dtype.Append('f');
                        dvals.AddRange(OscUtils.Pack(f));
                        break;

                    case string s:
                        dtype.Append('s');
                        dvals.AddRange(OscUtils.Pack(s));
                        break;

                    case List<byte> b:
                        dtype.Append('b');
                        dvals.AddRange(OscUtils.Pack(b));
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
                bytes.AddRange(OscUtils.Pack(Address));
                dtype.Insert(0, ',');
                bytes.AddRange(OscUtils.Pack(dtype.ToString()));
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
        public static Message Unpack(byte[] bytes)
        {
            int index = 0;
            bool ok = true;
            List<string> errors = new List<string>();

            // Parse address.
            string address = null;
            if (ok)
            {
                ok = OscUtils.Unpack(bytes, ref index, ref address);
            }
            if (!ok)
            {
                errors.Add("Invalid address string");
            }

            // Parse data types.
            string dtypes = null;
            if (ok)
            {
                ok = OscUtils.Unpack(bytes, ref index, ref dtypes);
            }
            if (ok)
            {
                ok = (dtypes.Length >= 1) && (dtypes[0] == ',');
            }

            // Parse data values.
            List<object> dvals = new List<object>();
            if (ok)
            {
                for (int i = 1; i < dtypes.Length && ok; i++)
                {
                    switch (dtypes[i])
                    {
                        case 'i':
                            int di = 0;
                            ok = OscUtils.Unpack(bytes, ref index, ref di);
                            if (ok)
                            {
                                dvals.Add(di);
                            }
                            break;

                        case 'f':
                            float df = 0;
                            ok = OscUtils.Unpack(bytes, ref index, ref df);
                            if (ok)
                            {
                                dvals.Add(df);
                            }
                            break;

                        case 's':
                            string ds = "";
                            ok = OscUtils.Unpack(bytes, ref index, ref ds);
                            if (ok)
                            {
                                dvals.Add(ds);
                            }
                            break;

                        case 'b':
                            List<byte> db = new List<byte>();
                            ok = OscUtils.Unpack(bytes, ref index, ref db);
                            if (ok)
                            {
                                dvals.Add(db);
                            }
                            break;

                        default:
                            ok = false;
                            errors.Add($"Invalid data type: {dtypes[i]}");
                            break;
                    }
                }
            }

            return new Message(address) { Data = dvals, Errors = errors };
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