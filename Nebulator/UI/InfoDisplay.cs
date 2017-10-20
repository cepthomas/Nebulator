using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Nebulator.Common;


namespace Nebulator.UI
{
    public partial class InfoDisplay : UserControl
    {
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
            txtInfo.BackColor = Globals.UserSettings.BackColor;
            txtInfo.Font = Globals.UserSettings.EditorFont;
            txtInfo.WordWrap = true;
            btnClear.Image = Utils.ColorizeBitmap(btnClear.Image);
            btnWrap.Image = Utils.ColorizeBitmap(btnWrap.Image);
            btnWrap.Checked = true;
        }

        /// <summary>
        /// A message to display to the user.
        /// </summary>
        /// <param name="msg">The message.</param>
        public void AddMessage(string msg)
        {
            if (txtInfo.TextLength > 5000)
            {
                txtInfo.Select(0, 1000);
                txtInfo.SelectedText = "";
            }

            txtInfo.AppendText(msg);
            //txtInfo.AppendText(msg + Environment.NewLine);
            txtInfo.ScrollToCaret();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnClear_Click(object sender, EventArgs e)
        {
            txtInfo.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnWrap_Click(object sender, EventArgs e)
        {
            txtInfo.WordWrap = btnWrap.Checked;
        }
    }
}
