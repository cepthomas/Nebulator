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
using MoreLinq;


namespace Nebulator.Common
{
    public static class Definitions //TODO clean/simplify these
    {
        /// <summary>General purpose marker.</summary>
        public const string UNKNOWN_STRING = "???";

        /// <summary>Indicates needs user involvement.</summary>
        public static Color ATTENTION_COLOR = Color.Red;

        /// <summary>Subdivision setting.</summary>
        public const int TOCKS_PER_TICK = 96;
    }

    public static class Utils
    {
        #region UI helpers
        /// <summary>
        /// Allows user to enter only integer or float values.
        /// s</summary>
        /// <param name="sender">Sender control.</param>
        /// <param name="e">Event args.</param>
        public static void TestForNumber_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Determine whether the keystroke is a number.
            char c = e.KeyChar;
            e.Handled = !((c >= '0' && c <= '9') || (c == '.') || (c == '\b') || (c == '-'));
        }

        /// <summary>
        /// Allows user to enter only integer values.
        /// </summary>
        /// <param name="sender">Sender control.</param>
        /// <param name="e">Event args.</param>
        public static void TestForInteger_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Determine whether the keystroke is a number.
            char c = e.KeyChar;
            e.Handled = !((c >= '0' && c <= '9') || (c == '\b') || (c == '-'));
        }

        /// <summary>Allows user to enter only alphanumeric values.</summary>
        /// <param name="sender">Sender control.</param>
        /// <param name="e">Event args.</param>
        public static void TestForAlphanumeric_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Determine whether the keystroke is a number.
            char c = e.KeyChar;
            e.Handled = !(char.IsLetterOrDigit(c) || (c == '\b') || (c == ' '));
        }
        #endregion

        #region System utils
        /// <summary>
        /// Returns a string with the application version information.
        /// </summary>
        public static string GetVersionString()
        {
            Version ver = typeof(Utils).Assembly.GetName().Version;
            return $"{ver.Major}.{ver.Minor}.{ver.Build}";
        }

        /// <summary>
        /// Get the user app dir.
        /// </summary>
        /// <returns></returns>
        public static string GetAppDir()
        {
            string localdir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localdir, "Nebulator");
        }

        /// <summary>
        /// Endian support.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static uint SwapUInt32(uint i)
        {
            return ((i & 0xFF000000) >> 24) | ((i & 0x00FF0000) >> 8) | ((i & 0x0000FF00) << 8) | ((i & 0x000000FF) << 24);
        }

        /// <summary>
        /// Endian support.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static ushort SwapUInt16(ushort i)
        {
            return (ushort)(((i & 0xFF00) >> 8) | ((i & 0x00FF) << 8));
        }

        /// <summary>
        /// Split a double into two parts: each side of the dp.
        /// </summary>
        /// <param name="val"></param>
        /// <returns>tuple of integral and fractional</returns>
        public static (double integral, double fractional) SplitDouble(double val)
        {
            double integral = Math.Truncate(val);
            double fractional = val - integral;
            return (integral, fractional);
        }

        /// <summary>
        /// Conversion.
        /// </summary>
        /// <param name="ticks"></param>
        /// <returns></returns>
        public static double TicksToMsec(long ticks)
        {
            return 1000.0 * ticks / Stopwatch.Frequency;
        }
        #endregion

        #region Colors
        /// <summary>
        /// Mix two colors.
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <returns></returns>
        public static Color HalfMix(Color one, Color two)
        {
            return Color.FromArgb(
                (one.A + two.A) >> 1,
                (one.R + two.R) >> 1,
                (one.G + two.G) >> 1,
                (one.B + two.B) >> 1);
        }

        /// <summary>Helper to get next contrast color in the sequence. From http://colorbrewer2.org qualitative.</summary>
        /// <param name="i"></param>
        /// <param name="dark">Dark or light series, usually dark.</param>
        /// <returns></returns>
        public static Color GetColor(int i, bool dark = true)
        {
            Color col = Color.Black;

            switch(i % 8)
            {
                case 0: col = dark ? Color.FromArgb(27, 158, 119) : Color.FromArgb(141, 211, 199); break;
                case 1: col = dark ? Color.FromArgb(217, 95, 2) : Color.FromArgb(255, 255, 179); break;
                case 2: col = dark ? Color.FromArgb(117, 112, 179) : Color.FromArgb(190, 186, 218); break;
                case 3: col = dark ? Color.FromArgb(231, 41, 138) : Color.FromArgb(251, 128, 114); break;
                case 4: col = dark ? Color.FromArgb(102, 166, 30) : Color.FromArgb(128, 177, 211); break;
                case 5: col = dark ? Color.FromArgb(230, 171, 2) : Color.FromArgb(253, 180, 98); break;
                case 6: col = dark ? Color.FromArgb(166, 118, 29) : Color.FromArgb(179, 222, 105); break;
                case 7: col = dark ? Color.FromArgb(102, 102, 102) : Color.FromArgb(252, 205, 229); break;
            }

            return col;
        }
        #endregion

        #region Image processing
        /// <summary>Resize the image to the specified width and height.</summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            //a holder for the result
            Bitmap result = new Bitmap(width, height);

            // set the resolutions the same to avoid cropping due to resolution differences
            result.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            //use a graphics object to draw the resized image into the bitmap
            using (Graphics graphics = Graphics.FromImage(result))
            {
                // set the resize quality modes to high quality
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                // draw the image into the target bitmap
                graphics.DrawImage(image, 0, 0, result.Width, result.Height);
            }

            //return the resulting bitmap
            return result;
        }

        /// <summary>
        /// Colorize a bitmap.
        /// </summary>
        /// <param name="original"></param>
        /// <param name="newcol"></param>
        /// <returns></returns>
        public static Bitmap ColorizeBitmap(Image original, Color newcol)
        {
            Bitmap origbmp = original as Bitmap;
            Bitmap newbmp = new Bitmap(original.Width, original.Height);

            for (int y = 0; y < newbmp.Height; y++)
            {
                for (int x = 0; x < newbmp.Width; x++)
                {
                    // Get the pixel from the image.
                    Color acol = origbmp.GetPixel(x, y);

                    // Test for not background.
                    if (acol.A > 0)
                    {
                        Color c = Color.FromArgb(acol.A, newcol.R, newcol.G, newcol.B);
                        newbmp.SetPixel(x, y, c);
                    }
                }
            }

            return newbmp;
        }
        #endregion

        #region Math helpers
        /// <summary>Conversion.</summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static float DegreesToRadians(float angle)
        {
            return (float)(Math.PI * angle / 180.0);
        }

        /// <summary>Conversion.</summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static float RadiansToDegrees(float angle)
        {
            return (float)(angle * 180.0 / Math.PI);
        }

        /// <summary>
        /// Remap a value to new coordinates.
        /// </summary>
        /// <param name="val"></param>
        /// <param name="start1"></param>
        /// <param name="stop1"></param>
        /// <param name="start2"></param>
        /// <param name="stop2"></param>
        /// <returns></returns>
        public static double Map(double val, double start1, double stop1, double start2, double stop2)
        {
            return start2 + (stop2 - start2) * (val - start1) / (stop1 - start1);
        }

        /// <summary>
        /// Calculate a Standard Deviation based on a List of doubles.
        /// </summary>
        /// <param name="inputArray">List of doubles</param>
        /// <returns>Double value of the Standard Deviation</returns>
        public static double StandardDeviation(List<double> inputArray)
        {
            double sd = double.NaN;

            if (inputArray.Count > 1)
            {
                double sumOfSquares = SumOfSquares(inputArray);
                sd = sumOfSquares / (inputArray.Count - 1);
            }
            else // Divide by Zero
            {
                sd = double.NaN;
            }
            if (sd < 0) // Square Root of Neg Number
            {
                sd = double.NaN;
            }

            sd = Math.Sqrt(sd); // Square Root of sd
            return sd;
        }

        /// <summary>
        /// Calculate a Sum of Squares given a List of doubles.
        /// </summary>
        /// <param name="inputArray">List of doubles</param>
        /// <returns>Double value of the Sum of Squares</returns>
        public static double SumOfSquares(List<double> inputArray)
        {
            double mean;
            double sumOfSquares;
            sumOfSquares = 0;

            mean = inputArray.Average();

            foreach (double v in inputArray)
            {
                sumOfSquares += Math.Pow((v - mean), 2);
            }
            return sumOfSquares;
        }

        /// <summary>
        /// Generates normally distributed numbers.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="mean">Mean</param>
        /// <param name="sigma">Sigma</param>
        /// <returns></returns>
        public static double NextGaussian(Random r, double mean = 0, double sigma = 1)
        {
            var u1 = r.NextDouble();
            var u2 = r.NextDouble();
            var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            var randNormal = mean + sigma * randStdNormal;
            return randNormal;
        }

        /// <summary>
        /// Bounds limits a value.
        /// </summary>
        /// <param name="val"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static double Constrain(double val, double min, double max)
        {
            val = Math.Max(val, min);
            val = Math.Min(val, max);
            return val;
        }

        /// <summary>
        /// Bounds limits a value.
        /// </summary>
        /// <param name="val"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int Constrain(int val, int min, int max)
        {
            val = Math.Max(val, min);
            val = Math.Min(val, max);
            return val;
        }
        #endregion
    }

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
    }

    /// <summary>
    /// Custom renderer for toolstrip checkbox color.
    /// </summary>
    public class CheckBoxRenderer : ToolStripSystemRenderer
    {
        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            var btn = e.Item as ToolStripButton;

            if(!(btn is null) && btn.CheckOnClick && btn.Checked)
            {
                Rectangle bounds = new Rectangle(Point.Empty, e.Item.Size);
                e.Graphics.FillRectangle(new SolidBrush(UserSettings.TheSettings.SelectedColor), bounds);
            }
            else
            {
                base.OnRenderButtonBackground(e);
            }
        }
    }
}
