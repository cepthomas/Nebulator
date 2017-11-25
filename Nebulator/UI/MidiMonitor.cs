using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Nebulator.Common;

// TODO2 midi indicator/overload?

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
            btnMonIn.Image = Utils.ColorizeBitmap(btnMonIn.Image, Globals.UserSettings.IconColor);
            btnMonOut.Image = Utils.ColorizeBitmap(btnMonOut.Image, Globals.UserSettings.IconColor);
            btnClear.Image = Utils.ColorizeBitmap(btnClear.Image, Globals.UserSettings.IconColor);
            btnKill.Image = Utils.ColorizeBitmap(btnKill.Image, Globals.UserSettings.IconColor);
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

            txtMonitor.AppendText($"{Globals.CurrentStepTime} {msg}{Environment.NewLine}");
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
    }
}
