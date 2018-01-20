using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;

namespace Nebulator.Common
{
    public static class Utils
    {
        #region Constants
        /// <summary>General purpose marker.</summary>
        public const string UNKNOWN_STRING = "???";

        /// <summary>Indicates needs user involvement.</summary>
        public static Color ATTENTION_COLOR = Color.Red;

        /// <summary>Subdivision setting.</summary>
        public const int TOCKS_PER_TICK = 96;
        #endregion

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
                    if(acol.A > 0)
                    {
                        Color c = Color.FromArgb(acol.A, newcol.R, newcol.G, newcol.B);
                        newbmp.SetPixel(x, y, c);
                    }
                }
            }

            return newbmp;
        }
        #endregion

        #region Misc utils
        /// <summary>
        /// Returns a string with the application version information.
        /// </summary>
        public static string GetVersionString()
        {
            Version ver = typeof(Utils).Assembly.GetName().Version;
            string ret = $"{ver.Major}.{ver.Minor}.{ver.Build}";
            return ret;
        }

        /// <summary>
        /// Get the user app dir.
        /// </summary>
        /// <returns></returns>
        public static string GetAppDir()
        {
            string localdir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appDir = Path.Combine(localdir, "Nebulator");
            return appDir;
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

        #region Things that have nothing to do with this project really
        /// <summary>
        /// Partial extraction of API for the wiki doc.
        /// </summary>
        /// <param name="fn"></param>
        public static void ExtractAPI(string fn)
        {
            List<string> all = new List<string>();
            List<string> part = new List<string>();
            bool firstRegion = true;

            foreach (string l in File.ReadAllLines(fn))
            {
                string s = l.Trim();

                if (s.StartsWith("#region"))
                {
                    s = s.Remove(0, 7).Trim();

                    if(firstRegion)
                    {
                        firstRegion = false;
                    }
                    else
                    {
                        // Sort accumulated by property or function name.
                        part.Sort((x, y) =>
                        {
                            List<string> lsx = x.SplitByTokens(" (");
                            List<string> lsy = y.SplitByTokens(" (");
                            lsx.Remove("virtual");
                            lsy.Remove("virtual");
                            return lsx[1].CompareTo(lsy[1]);
                        });

                        if(part.Count > 0)
                        {
                            all.AddRange(part);
                        }
                        else
                        {
                            all.Add("None implemented.");
                        }
                        part.Clear();
                        all.Add("```");
                        all.Add("");
                    }

                    all.Add("# " + s);
                    all.Add("```c#");
                }

                if (s.StartsWith("public "))
                {
                    s = s.Remove(0, 7);

                    int i = s.IndexOf('{');
                    if(i != -1)
                    {
                        s = s.Left(i);
                    }

                    s.Trim();

                    if(l.Contains("NotSupportedException"))
                    {
                        s = s.Insert(0, "//NI ");
                    }
                    else
                    {
                        part.Add(s);
                    }
                }
            }

            all.Add("```");

            Clipboard.SetText(string.Join(Environment.NewLine, all));
        }

        /// <summary>
        /// Utility to dump contents of dir structure.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="els"></param>
        public static void DumpDir(string path, List<string> els)
        {
            //List<string> els = new List<string>();
            DirectoryInfo di = new DirectoryInfo(path);

            foreach (FileInfo info in di.GetFiles())
            {
                if (info.Length > 10000000)
                {
                    //els.Add($"{0}: {1}", info.Length / 1000000, info.FullName));
                    Console.WriteLine($"{info.Length / 1000000}: {info.FullName}");
                }
            }

            foreach (DirectoryInfo info in di.GetDirectories())
            {
                DumpDir(info.FullName, els);
            }
        }
        #endregion

        #region Char conversion
        /// <summary>
        /// General purpose decoder for keys. Because windows makes it kind of difficult.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="modifiers"></param>
        /// <returns>Converted char - 0 if not convertible, and keyCode(s).</returns>
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
        #endregion
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
