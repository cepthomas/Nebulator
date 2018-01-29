using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using Markdig;
using Nebulator.Common;


namespace Nebulator.UI
{
    /// <summary>
    /// Cheesy about page.
    /// </summary>
    public partial class About : Form
    {
        /// <summary>
        /// Construction.
        /// </summary>
        public About()
        {
            InitializeComponent();
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }

        /// <summary>
        /// Initializer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void About_Load(object sender, EventArgs e)
        {
            BackColor = UserSettings.TheSettings.BackColor;

            picBox.BackgroundImage = Properties.Resources.medusa2;
            picBox.BackgroundImageLayout = ImageLayout.Center;
            picBox.BackColor = Color.Transparent;
            picBox.BringToFront();

            string s = Markdown.ToHtml(File.ReadAllText(@"Resources\README.md"));
            // Insert some style.
            s = s.Insert(0, $"<style>body {{ background - color: {UserSettings.TheSettings.BackColor.Name}; }}</style>");
            browser.DocumentText = s;

            timer1.Start();
        }

        /// <summary>
        /// Cheesy animation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            //int x = picBox.Location.X > Width ? 10 : picBox.Location.X;
            int x = picBox.Location.X;
            picBox.Location = new Point(x + 5, picBox.Location.Y);
        }
    }
}
