using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Nebulator.Common;


namespace Nebulator.UI
{
    public partial class TextViewer : Form
    {
        Color VALID_COLOR = Color.LightGreen;
        Color INVALID_COLOR = Color.Pink;
        Color WARN_COLOR = Color.Plum;
        Color NEUTRAL_COLOR = SystemColors.Control;

        /// <summary>
        /// The text to display.
        /// </summary>
        public List<string> Lines { get; set; } = new List<string>();

        /// <summary>
        /// Constructor.
        /// </summary>
        public TextViewer()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initialize everything.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextViewer_Load(object sender, EventArgs e)
        {
            ClientSize = new Size(900, 500);
            txtView.Font = Globals.UserSettings.EditorFont;
            txtView.BackColor = Globals.UserSettings.BackColor;

            using (new WaitCursor())
            {
                foreach (string l in Lines)
                {
                    switch(l)
                    {
                        case string s when s.Contains("|ERROR|"):
                            txtView.SelectionBackColor = INVALID_COLOR;
                            break;

                        case string s when s.Contains("|_WARN|"):
                            txtView.SelectionBackColor = WARN_COLOR;
                            break;

                        case string s when s.Contains("|_INFO|"):
                            //txtView.SelectionBackColor = VALID_COLOR;
                            //break;
                        default:
                            txtView.SelectionBackColor = NEUTRAL_COLOR;
                            break;
                    }

                    txtView.AppendText(l + Environment.NewLine);
                }
            }
        }
    }
}
