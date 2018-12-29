using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Text;
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
        /// Extra info about user's system.
        /// </summary>
        public string SysInfo { get; set; } = "???";

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
            string sall = SysInfo + Environment.NewLine + File.ReadAllText(@"Resources\README.md");
            string smd = Markdown.ToHtml(sall);
            // Insert some style.
            smd = smd.Insert(0, $"<style>body {{ background - color: {UserSettings.TheSettings.BackColor.Name}; font-family: \"Arial\", Helvetica, sans-serif; }}</style>");
            browser.DocumentText = smd;
        }
    }
}
