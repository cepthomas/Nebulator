using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Nebulator.Common;


namespace Nebulator.UI
{
    public partial class MidiMonitor : UserControl
    {
        /// <summary>
        /// 
        /// </summary>
        public MidiMonitor()
        {
            InitializeComponent();
            toolStrip1.Renderer = new Common.CheckBoxRenderer(); // for checked color.
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MidiMonitor_Load(object sender, EventArgs e)
        {
            txtMonitor.Font = Globals.UserSettings.EditorFont;
            txtMonitor.BackColor = Globals.UserSettings.BackColor;

            btnMonIn.Checked = Globals.UserSettings.MidiMonitorIn;
            btnMonOut.Checked = Globals.UserSettings.MidiMonitorOut;
            btnMonIn.Image = Utils.ColorizeBitmap(btnMonIn.Image);
            btnMonOut.Image = Utils.ColorizeBitmap(btnMonOut.Image);
            btnClear.Image = Utils.ColorizeBitmap(btnClear.Image);
        }

        /// <summary>
        /// A message to display to the user.
        /// </summary>
        /// <param name="msg">The message.</param>
        public void AddMidiMessage(string msg)
        {
            if (txtMonitor.TextLength > 5000)
            {
                txtMonitor.Select(0, 1000);
                txtMonitor.SelectedText = "";
            }

            string s = $"{Globals.CurrentStepTime} {msg} {Environment.NewLine}";
            txtMonitor.AppendText(s);
            txtMonitor.ScrollToCaret();
        }

        /// <summary>
        /// 
        /// </summary>
        private void BtnClear_Click(object sender, EventArgs e)
        {
            txtMonitor.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        private void BtnMonIn_Click(object sender, EventArgs e)
        {
            Globals.UserSettings.MidiMonitorIn = btnMonIn.Checked;
        }

        /// <summary>
        /// 
        /// </summary>
        private void BtnMonOut_Click(object sender, EventArgs e)
        {
            Globals.UserSettings.MidiMonitorOut = btnMonOut.Checked;
        }
    }
}
