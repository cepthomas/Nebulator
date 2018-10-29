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
    /// A part of an OSC Address Pattern matches a part of an OSC Address if every consecutive character in the OSC Address Pattern
    /// matches the next consecutive substring of the OSC Address and every character in the OSC Address is matched by something in
    /// the OSC Address Pattern.These are the matching rules for characters in the OSC Address Pattern:
    ///    - '?' in the OSC Address Pattern matches any single character
    ///    - '*' in the OSC Address Pattern matches any sequence of zero or more characters
    ///    - A string of characters in square brackets (e.g., "[string]") in the OSC Address Pattern matches any character in the string.
    ///      Inside square brackets, the minus sign(-) and exclamation point(!) have special meanings:
    ///        - two characters separated by a minus sign indicate the range of characters between the given two in ASCII collating sequence.
    ///          (A minus sign at the end of the string has no special meaning.)
    ///        - An exclamation point at the beginning of a bracketed string negates the sense of the list, meaning that the list matches
    ///          any character not in the list. (An exclamation point anywhere besides the first character after the open bracket has no
    ///          special meaning.)
    ///    - A comma-separated list of strings enclosed in curly braces(e.g., "{foo,bar}") in the OSC Address Pattern matches any of the
    ///      strings in the list.
    ///    - Any other character in an OSC Address Pattern can match only the same character.
    /// </summary>
    public class Address
    {
        #region Properties
        /// <summary>The OSC time tag.</summary>
        public string Raw { get; private set; } = Utils.UNKNOWN_STRING;
        #endregion

        #region Fields
        /// <summary>The pattern to use for matching by client.</summary>
        readonly string _pattern = "";

        /// <summary>Pattern is regex.</summary>
        readonly bool _isRegex = false;
        #endregion

        #region Public functions
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="raw">Address to be matched.</param>
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
                    case '[':
                        sb.Append('[');
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

        /// <summary>
        /// Client calls to test for match.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public bool IsMatch(string address)
        {
            return _isRegex ? Regex.IsMatch(address, _pattern) : address == _pattern;
        }
        #endregion
    }
}