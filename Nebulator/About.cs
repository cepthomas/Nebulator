using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using Markdig;
using Nebulator.Common;


namespace Nebulator
{
    /// <summary>
    /// About page.
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

            string s = Markdown.ToHtml(File.ReadAllText(@"Resources\README.md"));
            // Insert some style.
            s = s.Insert(0, $"<style>body {{ background - color: {UserSettings.TheSettings.BackColor.Name}; }}</style>");
            browser.DocumentText = s;
        }
    }
}
