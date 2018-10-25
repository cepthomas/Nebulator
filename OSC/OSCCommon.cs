using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections; //TODO remove
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
    /// Time tags are represented by a 64 bit fixed point number. The first 32 bits specify the number of seconds since midnight
    /// on January 1, 1900, and the last 32 bits specify fractional parts of a second to a precision of about 200 picoseconds.
    /// This is the representation used by Internet NTP timestamps.
    /// The time tag value consisting of 63 zero bits followed by a one in the least signifigant bit is a special case meaning "immediately."
    /// </summary>
    public class TimeTag
    {
        //var(integral, fractional) = Utils.SplitDouble(tts);
        //Tick = (int) integral;
        //Tock = (int) (fractional* 100);

        uint Seconds { get; set; }

        uint SecondsFraction { get; set; }

        byte[] Pack()
        {
            return null;
        }

        void Unpack(byte[] bytes)
        {

        }
    }

    public class Data
    {
        byte[] Pack()
        {
            return null;
        }

        void Unpack(byte[] bytes)
        {

        }

    }

    /// <summary>
    /// Contains an address, a comma followed by one or more type identifiers. then the data itself follows in binary encoding.
    /// An OSC Address Pattern is an OSC-string beginning with the character '/' (forward slash).
    /// An OSC Type Tag String is an OSC-string beginning with the character ',' (comma) followed by a sequence of characters
    /// corresponding exactly to the sequence of OSC Arguments in the given message. Each character after the comma is called an
    /// OSC Type Tag and represents the type of the corresponding OSC Argument. (The requirement for OSC Type Tag Strings to 
    /// start with a comma makes it easier for the recipient of an OSC Message to determine whether that OSC Message is lacking 
    /// an OSC Type Tag String.)
    /// </summary>
    public class Message
    {
        string Address { get; set; }

        byte[] Pack()
        {
            return null;
        }

        void Unpack(byte[] bytes)
        {

        }
    }

    /// <summary>
    /// An OSC Bundle consists of the OSC-string "#bundle" followed by an OSC Time Tag, followed by zero or more OSC Bundle Elements.
    /// The OSC-timetag is a 64-bit fixed point time tag whose semantics are described below.
    /// An OSC Bundle Element consists of its size and its contents.The size is an int32 representing the number of 8-bit bytes in
    /// the contents, and will always be a multiple of 4. The contents are either an OSC Message or an OSC Bundle.
    /// Note this recursive definition: bundle may contain bundles.
    /// </summary>
    public class Bundle
    {
        const string BUNDLE_ID = "#bundle";

        TimeTag TimeTag { get; set; } = new TimeTag();

        List<Bundle> Bundles { get; set; } = new List<Bundle>();

        List<Message> Messages { get; set; } = new List<Message>();

        byte[] Pack()
        {
            return null;
        }

        void Unpack(byte[] bytes)
        {

        }

    }
}