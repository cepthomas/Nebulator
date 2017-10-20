
using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using System.Drawing;


namespace Nebulator.Common
{
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

        /// <summary>
        /// Colorize a bitmap.
        /// </summary>
        /// <param name="original"></param>
        /// <param name="newcol"></param>
        /// <returns></returns>
        public static Bitmap ColorizeBitmap(Bitmap original, Color newcol)
        {
            Bitmap newbmp = null;

            newbmp = new Bitmap(original.Width, original.Height);

            for (int i = 0; i < newbmp.Width; i++)
            {
                for (int j = 0; j < newbmp.Height; j++)
                {
                    // Get the pixel from the image.
                    Color acol = original.GetPixel(i, j);
                    Color c = Color.FromArgb(acol.A, acol.R == 0 ? newcol.R : 255, acol.G == 0 ? newcol.G : 255, acol.B == 0 ? newcol.B : 255);
                    newbmp.SetPixel(i, j, c);
                }
            }

            return newbmp;
        }

        /// <summary>
        /// Colorize using user defined color.
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        public static Bitmap ColorizeBitmap(Image original)
        {
            return ColorizeBitmap(original as Bitmap, Globals.UserSettings.IconColor);
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
        /// Utility to dump contents of dir structure. Nothing to do with this project really.
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
        /// <returns>tuple of integral, fractional</returns>
        public static (int integral, int fractional) Split(double val)
        {
            // Split into two parts from 0.01 or 1.01 or 1.10.
            int integral = (int)Math.Truncate(val);
            int fractional = (int)((val - integral) * 1000);
            return (integral, fractional);
        }
        #endregion

        #region Math helpers
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
                e.Graphics.FillRectangle(new SolidBrush(Globals.UserSettings.SelectedColor), bounds);
            }
            else
            {
                base.OnRenderButtonBackground(e);
            }
        }
    }
}
