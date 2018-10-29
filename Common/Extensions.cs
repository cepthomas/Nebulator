using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices;
using System.ComponentModel;
using MoreLinq;


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
            sourceString.ForEach(c => isint &= char.IsNumber(c));
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
            sourceString.ForEach(c => isalpha &= char.IsLetter(c));
            return isalpha;
        }

        /// <summary>
        /// Returns the rightmost characters of a string based on the number of characters specified.
        /// </summary>
        /// <param name="str">The source string to return characters from.</param>
        /// <param name="numChars">The number of rightmost characters to return.</param>
        /// <returns>The rightmost characters of a string.</returns>
        public static string Right(this string str, int numChars)
        {
            numChars = Utils.Constrain(numChars, 0, str.Length);
            return str.Substring(str.Length - numChars, numChars);
        }

        /// <summary>
        /// Returns the leftmost number of chars in the string.
        /// </summary>
        /// <param name="str">The source string .</param>
        /// <param name="numChars">The number of characters to get from the source string.</param>
        /// <returns>The leftmost number of characters to return from the source string supplied.</returns>
        public static string Left(this string str, int numChars)
        {
            numChars = Utils.Constrain(numChars, 0, str.Length);
            return str.Substring(0, numChars);
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
            var ret = new List<string>();
            var list = text.Split(tokens.ToCharArray(), trim ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
            list.ForEach(s => ret.Add(trim ? s.Trim() : s));
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
            var ret = new List<string>();
            var list = text.Split(new string[] { splitby }, trim ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
            list.ForEach(s => ret.Add(trim ? s.Trim() : s));
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
        /// Get a subset of an array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="start"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static T[] Subset<T>(this T[] source, int start, int length)
        {
            T[] subset = new T[length];
            Array.Copy(source, start, subset, 0, length);
            return subset;
        }
    }
}
