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


/*

The unit of transmission of OSC is an OSC Packet. Any application that sends OSC Packets is an OSC Client; any application that receives OSC Packets
is an OSC Server.

An OSC packet consists of its contents, a contiguous block of binary data, and its size, the number of 8-bit bytes that comprise the contents.
The size of an OSC packet is always a multiple of 4.

The contents of an OSC packet must be either an OSC Message or an OSC Bundle. The first byte of the packet's contents unambiguously distinguishes between these two alternatives.

An OSC message consists of an OSC Address Pattern followed by an OSC Type Tag String followed by zero or more OSC Arguments.

An OSC Address Pattern is an OSC-string beginning with the character '/' (forward slash).

An OSC Type Tag String is an OSC-string beginning with the character ',' (comma) followed by a sequence of characters corresponding exactly
to the sequence of OSC Arguments in the given message.

An OSC Bundle consists of the OSC-string "#bundle" followed by an OSC Time Tag, followed by zero or more OSC Bundle Elements.

An OSC Bundle Element consists of its size and its contents. The size is an int32 representing the number of 8-bit bytes in the contents, and
will always be a multiple of 4. The contents are either an OSC Message or an OSC Bundle.

Note this recursive definition: bundle may contain bundles.


OSC Messages in the same OSC Bundle are atomic; their corresponding OSC Methods should be invoked in immediate succession as if no other
processing took place between the OSC Method invocations.

When an OSC Address Pattern is dispatched to multiple OSC Methods, the order in which the matching OSC Methods are invoked is unspecified.
When an OSC Bundle contains multiple OSC Messages, the sets of OSC Methods corresponding to the OSC Messages must be invoked in the same order as the OSC Messages appear in the packet. (example)

When bundles contain other bundles, the OSC Time Tag of the enclosed bundle must be greater than or equal to the OSC Time Tag of the enclosing
bundle. The atomicity requirement for OSC Messages in the same OSC Bundle does not apply to OSC Bundles within an OSC Bundle.

*/



namespace Nebulator.OSC
{
    /// <summary>
    /// Time tags are represented by a 64 bit fixed point number. The first 32 bits specify the number of seconds since midnight
    /// on January 1, 1900, and the last 32 bits specify fractional parts of a second to a precision of about 200 picoseconds.
    /// This is the representation used by Internet NTP timestamps.
    /// The time tag value consisting of 63 zero bits followed by a one in the least signifigant bit is a special case meaning "immediately."
    /// </summary>
    public class TimeTag
    {
        //ulong _ttVal = 0;

        uint Seconds { get; set; }

        uint SecondsFraction { get; set; }

        public List<byte> Pack()
        {
            return null; //TODOX
        }

        public void Unpack(List<byte> bytes)
        {

        }
    }

    public class Address
    {
        public string Raw { get; private set; } = Utils.UNKNOWN_STRING;

        string _pattern = "";
        bool _isRegex = false;

        // A part of an OSC Address Pattern matches a part of an OSC Address if every consecutive character in the OSC Address Pattern
        // matches the next consecutive substring of the OSC Address and every character in the OSC Address is matched by something in
        // the OSC Address Pattern.These are the matching rules for characters in the OSC Address Pattern:
        //    - '?' in the OSC Address Pattern matches any single character
        //    - '*' in the OSC Address Pattern matches any sequence of zero or more characters
        //    - A string of characters in square brackets (e.g., "[string]") in the OSC Address Pattern matches any character in the string.
        //      Inside square brackets, the minus sign(-) and exclamation point(!) have special meanings:
        //        - two characters separated by a minus sign indicate the range of characters between the given two in ASCII collating sequence.
        //          (A minus sign at the end of the string has no special meaning.)
        //        - An exclamation point at the beginning of a bracketed string negates the sense of the list, meaning that the list matches
        //          any character not in the list. (An exclamation point anywhere besides the first character after the open bracket has no
        //          special meaning.)
        //    - A comma-separated list of strings enclosed in curly braces(e.g., "{foo,bar}") in the OSC Address Pattern matches any of the
        //      strings in the list.
        //    - Any other character in an OSC Address Pattern can match only the same character.
        public Address(string raw)
        {
            Raw = raw;

            bool chars = false;
            StringBuilder sb = new StringBuilder();

            foreach (var c in raw)
            {
                switch (c)
                {
                    // simple match
                    case '?':
                        sb.Append('.');
                        _isRegex = true;
                        break;

                    case '*':
                        sb.Append(".*");
                        _isRegex = true;
                        break;

                    // char list
                    case '[': sb.Append('[');
                        chars = true;
                        _isRegex = true;
                        break;

                    case ']':
                        sb.Append(']');
                        chars = false;
                        break;

                    //case '-': TODOX

                    case '!':
                        sb.Append(chars ? '^' : c);
                        break;

                    // string list
                    case '{':
                        sb.Append('(');
                        _isRegex = true;
                        break;

                    case '}':
                        sb.Append('|');
                        break;

                    case ',':
                        sb.Append(')');
                        break;

                    // everything else
                    default:
                        sb.Append(c);
                        break;
                }
            }

            _pattern = sb.ToString();
        }

        public bool IsMatch(string address)
        {
            return _isRegex ? Regex.IsMatch(address, _pattern) : address == _pattern;
        }
    }

    /// <summary>
    /// Message contains an address, a comma followed by one or more type identifiers, then the data itself follows in binary encoding.
    /// </summary>
    public class Message
    {
        public Address Address { get; private set; } = null;

        public List<object> Data { get; private set; } = new List<object>();

        public Message(string address)
        {
            Address = new Address(address);
        }

        public List<byte> Pack()
        {
            // Data type string.
            StringBuilder sb = new StringBuilder(",");

            // Data values.
            List<byte> dvals = new List<byte>();

            // These Attributes adhere to the OSC Specs 1.0
            // i = int32 = 32-bit big-endian two's complement integer
            // f = float32 = 32-bit big-endian IEEE 754 floating point number
            // s = OSC-string = A sequence of non-null ASCII characters followed by a null, followed by 0-3 additional null characters
            //     to make the total number of bits a multiple of 32.
            // b = OSC-blob = An int32 size count, followed by that many 8-bit bytes of arbitrary binary data, followed by 0-3 
            //     additional zero bytes to make the total number of bits a multiple of 32.

            //Some OSC applications communicate among instances of themselves with additional, nonstandard argument types beyond
            // those specified above. OSC applications are not required to recognize these types; an OSC application should 
            // discard any message whose OSC Type Tag String contains any unrecognized OSC Type Tags. An application that does 
            // use any additional argument types must encode them with the OSC Type Tags in this table:
            //h   64 bit big-endian two's complement integer
            //t   OSC-timetag
            //d   64 bit ("double") IEEE 754 floating point number
            //S   Alternate type represented as an OSC-string (for example, for systems that differentiate "symbols" from "strings")
            //c   an ascii character, sent as 32 bits
            //r   32 bit RGBA color
            //m   4 byte MIDI message. Bytes from MSB to LSB are: port id, status byte, data1, data2
            //T   True. No bytes are allocated in the argument data.
            //F   False. No bytes are allocated in the argument data.
            //N   Nil. No bytes are allocated in the argument data.
            //I   Infinitum. No bytes are allocated in the argument data.
            //[   Indicates the beginning of an array. The tags following are for data in the Array until a close brace tag is reached.
            //]   Indicates the end of an array.

            Data.ForEach(o =>
            {
                switch(o)
                {
                    case int i:
                        sb.Append('i');
                        dvals.AddRange(OSCUtils.Pack(i));
                        break;

                    case float f:
                        sb.Append('f');
                        dvals.AddRange(OSCUtils.Pack(f));
                        break;

                    case string s:
                        sb.Append('s');
                        dvals.AddRange(OSCUtils.PackString(s));
                        break;
                         
                    case List<byte> b:
                        sb.Append('b');
                        dvals.AddRange(b);
                        break;

                    // TODO the others?

                    default:
                        throw new Exception($"Unknown type");
                }
            });

            // Put it all together.
            List<byte> bytes = new List<byte>();
            bytes.AddRange(OSCUtils.PackString(Address.Raw));
            bytes.AddRange(dvals);

            return bytes;
        }

        public void Unpack(List<byte> bytes)
        {

        }
    }

    /// <summary>
    /// An OSC Bundle consists of the OSC-string "#bundle" followed by an OSC Time Tag, followed by zero or more OSC Message or Bundle Elements.
    /// The OSC-timetag is a 64-bit fixed point time tag whose semantics are described below.
    /// An OSC Bundle Element consists of its size and its contents. The size is an int32 representing the number of 8-bit bytes in
    ///   the contents, and will always be a multiple of 4. The contents are either an OSC Message or an OSC Bundle.
    /// Note this recursive definition: bundle may contain bundles.
    /// </summary>
    public class Bundle
    {
        public const string BUNDLE_ID = "#bundle";

        public TimeTag TimeTag { get; private set; } = new TimeTag();

        public List<Bundle> Bundles { get; private set; } = new List<Bundle>();

        public List<Message> Messages { get; private set; } = new List<Message>();

        public Bundle(TimeTag ttag)
        {
            TimeTag = ttag;
        }

        public List<byte> Pack()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(OSCUtils.PackString(BUNDLE_ID));
            bytes.AddRange(TimeTag.Pack());
            Bundles.ForEach(b => bytes.AddRange(b.Pack()));
            Messages.ForEach(m => bytes.AddRange(m.Pack()));
            bytes.Pad();
            return bytes;
        }

        public void Unpack(List<byte> bytes)
        {

        }
    }

    public static class OSCUtils
    {
        public static CommCaps InitCaps()
        {
            CommCaps caps = new CommCaps() //TODOX
            {

            };

            return caps;
        }

        public static List<byte> PackString(string value)
        {
            List<byte> bytes = new List<byte>();
            value.ToList().ForEach(v => bytes.AddRange(BitConverter.GetBytes(v)));
            bytes.Add(0);
            bytes.Pad();
            return bytes;
        }

        public static List<byte> Pack(int value)
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