using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NBagOfTricks;
using Nebulator.Common;

namespace Nebulator.Controls
{
    public partial class TextViewer : UserControl
    {
        /// <summary>
        /// The colors to display when text is matched.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Dictionary<string, Color> Colors { get; set; } = new Dictionary<string, Color>();

        /// <summary>
        /// Constructor.
        /// </summary>
        public TextViewer()
        {
            InitializeComponent();
            toolStrip1.Renderer = new Common.CheckBoxRenderer(); // for checked color.
        }

        /// <summary>
        /// Initialize everything.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextViewer_Load(object sender, EventArgs e)
        {
            txtView.Font = UserSettings.TheSettings.EditorFont;
            txtView.BackColor = UserSettings.TheSettings.BackColor;
            txtView.WordWrap = false;

            btnClear.Image = MiscUtils.ColorizeBitmap(btnClear.Image, UserSettings.TheSettings.IconColor);
            btnWrap.Image = MiscUtils.ColorizeBitmap(btnWrap.Image, UserSettings.TheSettings.IconColor);
            btnWrap.Checked = false;
        }

        /// <summary>
        /// A message to display to the user. Adds EOL.
        /// </summary>
        /// <param name="text">The message.</param>
        /// <param name="trim">True to truncate continuous displays.</param>
        public void AddLine(string text, bool trim = true)
        {
            Add(text + Environment.NewLine, trim);
        }

        /// <summary>
        /// A message to display to the user. Doesn't add EOL.
        /// </summary>
        /// <param name="text">The message.</param>
        /// <param name="trim">True to truncate continuous displays.</param>
        public void Add(string text, bool trim = true)
        {
            if (trim && txtView.TextLength > 5000)
            {
                txtView.Select(0, 1000);
                txtView.SelectedText = "";
            }

            txtView.SelectionBackColor = UserSettings.TheSettings.BackColor;

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
        /// 
        /// </summary>
        public void Clear()
        {
            txtView.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Clear_Click(object sender, EventArgs e)
        {
            Clear();
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
