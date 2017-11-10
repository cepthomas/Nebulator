using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Text.RegularExpressions;
using System.Drawing;


namespace Nebulator.Common
{
    public static class Extensions
    {
        /// <summary>
        /// Test for integer string.
        /// </summary>
        /// <param name="sourceString"></param>
        /// <returns></returns>
        public static bool IsInteger(this string sourceString)
        {
            bool isint = true;
            foreach (char c in sourceString)
            {
                isint &= char.IsNumber(c);
            }
            return isint;
        }

        /// <summary>
        /// Test for alpha string.
        /// </summary>
        /// <param name="sourceString"></param>
        /// <returns></returns>
        public static bool IsAlpha(this string sourceString)
        {
            bool isalpha = true;
            foreach (char c in sourceString)
            {
                isalpha &= char.IsLetter(c);
            }
            return isalpha;
        }

        /// <summary>
        /// Returns the rightmost characters of a string based on the number of characters specified.
        /// </summary>
        /// <param name="sourceString">The source string to return characters from.</param>
        /// <param name="numCharsToReturn">The number of rightmost characters to return.</param>
        /// <returns>The rightmost characters of a string.</returns>
        public static string Right(this string sourceString, int numCharsToReturn)
        {
            if ((numCharsToReturn < 1) || string.IsNullOrEmpty(sourceString))
            {
                // If 0 or less.
                sourceString = string.Empty;
            }
            else if (sourceString.Length >= numCharsToReturn)
            {
                // Valid criteria - Make the string no longer than the max length.
                sourceString = sourceString.Substring(sourceString.Length - numCharsToReturn, numCharsToReturn);
            }

            // Return the string
            return sourceString;
        }

        /// <summary>
        /// Returns the leftmost number of chars in the string.
        /// </summary>
        /// <param name="sourceString">The source string .</param>
        /// <param name="numCharsToReturn">The number of characters to get from the source string.</param>
        /// <returns>The leftmost number of characters to return from the source string supplied.</returns>
        public static string Left(this string sourceString, int numCharsToReturn)
        {
            string result = "";
            if (sourceString.Length > 0)
            {
                if (numCharsToReturn > sourceString.Length)
                {
                    numCharsToReturn = sourceString.Length;
                }

                result = sourceString.Substring(0, numCharsToReturn);
            }

            return result;
        }

        /// <summary>
        /// Splits a tokenized (delimited) string into its parts and optionally trims whitespace.
        /// </summary>
        /// <param name="text">The string to split up.</param>
        /// <param name="tokens">The char token(s) to split by.</param>
        /// <param name="trim">Remove whitespace at each end.</param>
        /// <returns>Return the parts as a list.</returns>
        public static List<string> SplitByTokens(this string text, string tokens, bool trim = true)
        {
            List<string> ret = new List<string>();

            char[] delimiters = new char[tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
            {
                delimiters[i] = tokens[i];
            }

            string[] list = text.Split(delimiters, trim ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
            foreach (string s in list)
            {
                ret.Add(trim ? s.Trim() : s);
            }

            return ret;
        }

        /// <summary>
        /// Splits a tokenized (delimited) string into its parts and optionally trims whitespace.
        /// </summary>
        /// <param name="text">The string to split up.</param>
        /// <param name="splitby">The string to split by.</param>
        /// <param name="trim">Remove whitespace at each end.</param>
        /// <returns>Return the parts as a list.</returns>
        public static List<string> SplitByToken(this string text, string splitby, bool trim = true)
        {
            List<string> ret = new List<string>();

            string[] list = text.Split(new string[] { splitby }, trim ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
            foreach (string s in list)
            {
                ret.Add(trim ? s.Trim() : s);
            }

            return ret;
        }

        /// <summary>
        /// Perform a blind deep copy of an object. The class must be marked as [Serializable] in order for this to work.
        /// There are many ways to do this: http://stackoverflow.com/questions/129389/how-do-you-do-a-deep-copy-an-object-in-net-c-specifically/11308879
        /// The binary serialization is apparently slower but safer. Feel free to reimplement with a better way.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T DeepClone<T>(this T obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;
                return (T)formatter.Deserialize(ms);
            }
        }

        /// <summary>
        /// Update the MRU.
        /// </summary>
        /// <param name="mruList">The MRU.</param>
        /// <param name="newVal">New value(s) to perhaps insert.</param>
        public static void UpdateMru(this List<string> mruList, string newVal)
        {
            // First check if it's already in there.
            for (int i = 0; i < mruList.Count; i++)
            {
                if (newVal == mruList[i])
                {
                    // Remove from current location so we can stuff it back in later.
                    mruList.Remove(mruList[i]);
                }
            }

            // Insert at the front and trim the tail.
            mruList.Insert(0, newVal);

            while (mruList.Count > 20)
            {
                mruList.RemoveAt(mruList.Count - 1);
            }
        }

        /// <summary>
        /// Rudimentary C# source code formatter.
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static List<string> FormatSourceCode(this List<string> src)
        {
            List<string> fmt = new List<string>();
            int indent = 0;

            foreach (string s in src)
            {
                if (s.StartsWith("{"))
                {
                    fmt.Add(new string(' ', indent * 4) + s);
                    indent++;
                }
                else if (s.StartsWith("}") && indent > 0)
                {
                    indent--;
                    fmt.Add(new string(' ', indent * 4) + s);
                }
                else
                {
                    fmt.Add(new string(' ', indent * 4) + s);
                }
            }

            return fmt;
        }
    }
}
