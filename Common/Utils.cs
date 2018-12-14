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
    /// <summary>
    /// Static utility functions.
    /// </summary>
    public static class Utils
    {
        #region Constants
        /// <summary>General purpose marker.</summary>
        public const string UNKNOWN_STRING = "???";
        #endregion

        #region UI helpers
        /// <summary>
        /// Allows user to enter only integer or double values.
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
        public static string GetAppDataDir()
        {
            string localdir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localdir, "Nebulator");
        }

        /// <summary>
        /// Get the executable dir.
        /// </summary>
        /// <returns></returns>
        public static string GetExeDir()
        {
            string sdir = Application.StartupPath;
            return sdir;
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

        /// <summary>General purpose decoder for keys. Because windows makes it kind of difficult.</summary>
        /// <param name="key"></param>
        /// <param name="modifiers"></param>
        /// <returns>Tuple of Converted char (0 if not convertible) and keyCode(s).</returns>
        public static (char ch, List<Keys> keyCodes) KeyToChar(Keys key, Keys modifiers)
        {
            char ch = (char)0;
            List<Keys> keyCodes = new List<Keys>();

            bool shift = modifiers.HasFlag(Keys.Shift);
            bool iscap = (Console.CapsLock && !shift) || (!Console.CapsLock && shift);

            // Check modifiers.
            if (modifiers.HasFlag(Keys.Control)) keyCodes.Add(Keys.Control);
            if (modifiers.HasFlag(Keys.Alt)) keyCodes.Add(Keys.Alt);
            if (modifiers.HasFlag(Keys.Shift)) keyCodes.Add(Keys.Shift);

            switch (key)
            {
                case Keys.Enter: ch = '\n'; break;
                case Keys.Tab: ch = '\t'; break;
                case Keys.Space: ch = ' '; break;
                case Keys.Back: ch = (char)8; break;
                case Keys.Escape: ch = (char)27; break;
                case Keys.Delete: ch = (char)127; break;

                case Keys.Left: keyCodes.Add(Keys.Left); break;
                case Keys.Right: keyCodes.Add(Keys.Right); break;
                case Keys.Up: keyCodes.Add(Keys.Up); break;
                case Keys.Down: keyCodes.Add(Keys.Down); break;

                case Keys.D0: ch = shift ? ')' : '0'; break;
                case Keys.D1: ch = shift ? '!' : '1'; break;
                case Keys.D2: ch = shift ? '@' : '2'; break;
                case Keys.D3: ch = shift ? '#' : '3'; break;
                case Keys.D4: ch = shift ? '$' : '4'; break;
                case Keys.D5: ch = shift ? '%' : '5'; break;
                case Keys.D6: ch = shift ? '^' : '6'; break;
                case Keys.D7: ch = shift ? '&' : '7'; break;
                case Keys.D8: ch = shift ? '*' : '8'; break;
                case Keys.D9: ch = shift ? '(' : '9'; break;

                case Keys.Oemplus: ch = shift ? '+' : '='; break;
                case Keys.OemMinus: ch = shift ? '_' : '-'; break;
                case Keys.OemQuestion: ch = shift ? '?' : '/'; break;
                case Keys.Oemcomma: ch = shift ? '<' : ','; break;
                case Keys.OemPeriod: ch = shift ? '>' : '.'; break;
                case Keys.OemQuotes: ch = shift ? '\"' : '\''; break;
                case Keys.OemSemicolon: ch = shift ? ':' : ';'; break;
                case Keys.OemPipe: ch = shift ? '|' : '\\'; break;
                case Keys.OemCloseBrackets: ch = shift ? '}' : ']'; break;
                case Keys.OemOpenBrackets: ch = shift ? '{' : '['; break;
                case Keys.Oemtilde: ch = shift ? '~' : '`'; break;

                case Keys.NumPad0: ch = '0'; break;
                case Keys.NumPad1: ch = '1'; break;
                case Keys.NumPad2: ch = '2'; break;
                case Keys.NumPad3: ch = '3'; break;
                case Keys.NumPad4: ch = '4'; break;
                case Keys.NumPad5: ch = '5'; break;
                case Keys.NumPad6: ch = '6'; break;
                case Keys.NumPad7: ch = '7'; break;
                case Keys.NumPad8: ch = '8'; break;
                case Keys.NumPad9: ch = '9'; break;
                case Keys.Subtract: ch = '-'; break;
                case Keys.Add: ch = '+'; break;
                case Keys.Decimal: ch = '.'; break;
                case Keys.Divide: ch = '/'; break;
                case Keys.Multiply: ch = '*'; break;

                default:
                    if (key >= Keys.A && key <= Keys.Z)
                    {
                        // UC is 65-90  LC is 97-122
                        ch = iscap ? ch = (char)(int)key : (char)(int)(key + 32);
                    }
                    break;
            }

            return (ch, keyCodes);
        }

        /// <summary>Key state query. Based on https://stackoverflow.com/a/9356006.</summary>
        /// <param name="key">Which key.</param>
        /// <returns></returns>
        public static KeyStates GetKeyState(Keys key)
        {
            KeyStates state = KeyStates.None;

            short retVal = GetKeyStateW32((int)key);

            // If the high-order bit is 1, the key is down otherwise, it is up.
            if ((retVal & 0x8000) == 0x8000)
            {
                state |= KeyStates.Down;
            }

            // If the low-order bit is 1, the key is toggled.
            if ((retVal & 1) == 1)
            {
                state |= KeyStates.Toggled;
            }

            return state;
        }
        [Flags]
        public enum KeyStates { None = 0, Down = 1, Toggled = 2 }
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern short GetKeyStateW32(int keyCode);
        #endregion

        #region Misc
        /// <summary>
        /// Conversion.
        /// </summary>
        /// <param name="ticks"></param>
        /// <returns></returns>
        public static double TicksToMsec(long ticks)
        {
            return 1000.0 * ticks / Stopwatch.Frequency;
        }

        /// <summary>Rudimentary C# source code formatter to make generated files somewhat readable.</summary>
        /// <param name="src">Lines to prettify.</param>
        /// <returns>Formatted lines.</returns>
        public static List<string> FormatSourceCode(List<string> src)
        {
            List<string> fmt = new List<string>();
            int indent = 0;

            src.ForEach(s =>
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
            });

            return fmt;
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

            for (int y = 0; y < newbmp.Height; y++) // This is not very effecient! Use a buffer...
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

        #region Colors - from Utils
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
        public static Color GetSequenceColor(int i, bool dark = true)
        {
            Color col = Color.Black;

            switch (i % 8)
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

        #region Math helpers
        /// <summary>Conversion.</summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static double DegreesToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }

        /// <summary>Conversion.</summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static double RadiansToDegrees(double angle)
        {
            return angle * 180.0 / Math.PI;
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
        /// Remap a value to new coordinates.
        /// </summary>
        /// <param name="val"></param>
        /// <param name="start1"></param>
        /// <param name="stop1"></param>
        /// <param name="start2"></param>
        /// <param name="stop2"></param>
        /// <returns></returns>
        public static int Map(int val, int start1, int stop1, int start2, int stop2)
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
}
