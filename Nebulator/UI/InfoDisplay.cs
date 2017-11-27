using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Nebulator.Common;


namespace Nebulator.UI
{
    public partial class InfoDisplay : UserControl
    {
        /// <summary>
        /// The colors to display when text is matched.
        /// </summary>
        [Browsable(false)]
        public Dictionary<string, Color> Colors { get; set; } = new Dictionary<string, Color>();

        /// <summary>
        /// 
        /// </summary>
        public InfoDisplay()
        {
            InitializeComponent();
            toolStrip1.Renderer = new Common.CheckBoxRenderer(); // for checked color.
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InfoDisplay_Load(object sender, EventArgs e)
        {
            txtView.Font = Globals.UserSettings.EditorFont;
            txtView.BackColor = Globals.UserSettings.BackColor;
            txtView.WordWrap = false;

            btnClear.Image = Utils.ColorizeBitmap(btnClear.Image, Globals.UserSettings.IconColor);

            btnWrap.Image = Utils.ColorizeBitmap(btnWrap.Image, Globals.UserSettings.IconColor);
            //btnWrap.Checked = true;

            btnMonIn.Checked = Globals.UserSettings.MidiMonitorIn;
            btnMonOut.Checked = Globals.UserSettings.MidiMonitorOut;
            btnMonIn.Image = Utils.ColorizeBitmap(btnMonIn.Image, Globals.UserSettings.IconColor);
            btnMonOut.Image = Utils.ColorizeBitmap(btnMonOut.Image, Globals.UserSettings.IconColor);
            btnClear.Image = Utils.ColorizeBitmap(btnClear.Image, Globals.UserSettings.IconColor);
            btnKill.Image = Utils.ColorizeBitmap(btnKill.Image, Globals.UserSettings.IconColor);
        }

        /// <summary>
        /// A message to display to the user. Adds EOL.
        /// </summary>
        /// <param name="text">The message.</param>
        /// <param name="trim">True to truncate continuous displays.</param>
        public void AddInfoLine(string text, bool trim = true)
        {
            AddInfo(text + Environment.NewLine, trim);
        }

        /// <summary>
        /// A message to display to the user. Doesn't add EOL.
        /// </summary>
        /// <param name="text">The message.</param>
        /// <param name="trim">True to truncate continuous displays.</param>
        public void AddInfo(string text, bool trim = true)
        {
            if (trim && txtView.TextLength > 5000)
            {
                txtView.Select(0, 1000);
                txtView.SelectedText = "";
            }

            txtView.SelectionBackColor = BackColor;

            foreach (string s in Colors.Keys)
            {
                if (text.Contains(s))
                {
                    txtView.SelectionBackColor = Colors[s];
                    break;
                }
            }

            txtView.AppendText(text);
            txtView.ScrollToCaret();
        }

        /// <summary>
        /// A message to display to the user.
        /// </summary>
        /// <param name="msg">The message.</param>
        public void AddMidiMessage(string msg)
        {
            AddInfoLine($"{Globals.CurrentStepTime} {msg}");
        }

        /// <summary>
        /// 
        /// </summary>
        private void BtnClear_Click(object sender, EventArgs e)
        {
            txtView.Clear();
        }

        /// <summary>
        /// Monitor midi in messages. Note that monitoring slows down processing so use judiciously.
        /// </summary>
        private void BtnMonIn_Click(object sender, EventArgs e)
        {
            Globals.UserSettings.MidiMonitorIn = btnMonIn.Checked;
        }

        /// <summary>
        /// Monitor midi out messages. Note that monitoring slows down processing so use judiciously.
        /// </summary>
        private void BtnMonOut_Click(object sender, EventArgs e)
        {
            Globals.UserSettings.MidiMonitorOut = btnMonOut.Checked;
        }

        /// <summary>
        /// Send a mdi kill all message.
        /// </summary>
        private void btnKill_Click(object sender, EventArgs e)
        {
            Globals.MidiInterface.KillAll();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Wrap_Click(object sender, EventArgs e)
        {
            txtView.WordWrap = btnWrap.Checked;
        }
    }
}
