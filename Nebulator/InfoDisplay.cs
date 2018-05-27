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

namespace Nebulator
{
    public partial class InfoDisplay : UserControl
    {
        #region Properties
        /// <summary>The colors to display when text is matched.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Dictionary<string, Color> Colors { get; set; } = new Dictionary<string, Color>();

        /// <summary>Limit the display size.</summary>
        public int MaxLength { get; set; } = 5000;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Construct.
        /// </summary>
        public InfoDisplay()
        {
            InitializeComponent();
            toolStrip1.Renderer = new Common.CheckBoxRenderer(); // for checked color.
        }

        /// <summary>
        /// Init.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InfoDisplay_Load(object sender, EventArgs e)
        {
            txtView.Font = UserSettings.TheSettings.EditorFont;
            txtView.BackColor = UserSettings.TheSettings.BackColor;
            txtView.WordWrap = UserSettings.TheSettings.WordWrap;

            btnClear.Image = Utils.ColorizeBitmap(btnClear.Image, UserSettings.TheSettings.IconColor);

            btnWrap.Image = Utils.ColorizeBitmap(btnWrap.Image, UserSettings.TheSettings.IconColor);
            //btnWrap.Checked = true;

            btnMonIn.Checked = UserSettings.TheSettings.MidiMonitorIn;
            btnMonOut.Checked = UserSettings.TheSettings.MidiMonitorOut;
            btnMonIn.Image = Utils.ColorizeBitmap(btnMonIn.Image, UserSettings.TheSettings.IconColor);
            btnMonOut.Image = Utils.ColorizeBitmap(btnMonOut.Image, UserSettings.TheSettings.IconColor);
            btnClear.Image = Utils.ColorizeBitmap(btnClear.Image, UserSettings.TheSettings.IconColor);
        }
        #endregion

        #region Public functions
        /// <summary>
        /// A message to display to the user. Doesn't add EOL.
        /// </summary>
        /// <param name="text">The message.</param>
        public void AddInfo(string text)
        {
            if (txtView.TextLength > MaxLength)
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
        #endregion

        /// <summary>
        /// 
        /// </summary>
        private void BtnClear_Click(object sender, EventArgs e)
        {
            txtView.Clear();
        }

        #region Button handlers
        /// <summary>
        /// Monitor midi in messages. Note that monitoring slows down processing so use judiciously.
        /// </summary>
        private void BtnMonIn_Click(object sender, EventArgs e)
        {
            UserSettings.TheSettings.MidiMonitorIn = btnMonIn.Checked;
        }

        /// <summary>
        /// Monitor midi out messages. Note that monitoring slows down processing so use judiciously.
        /// </summary>
        private void BtnMonOut_Click(object sender, EventArgs e)
        {
            UserSettings.TheSettings.MidiMonitorOut = btnMonOut.Checked;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Wrap_Click(object sender, EventArgs e)
        {
            UserSettings.TheSettings.WordWrap = btnWrap.Checked;
            txtView.WordWrap = btnWrap.Checked;
        }
        #endregion
    }
}
