using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Nebulator.Common
{
    /// <summary>
    /// General purpose container for persistence.
    /// </summary>
    [Serializable]
    public class FormInfo
    {
        public bool Visible { get; set; } = false;
        public int X { get; set; } = 50;
        public int Y { get; set; } = 50;
        public int Width { get; set; } = 1000;
        public int Height { get; set; } = 700;

        public void FromForm(Form f)
        {
            Visible = f.Visible;
            X = f.Location.X;
            Y = f.Location.Y;
            Width = f.Width;
            Height = f.Height;
        }
    }

    /// <summary>
    /// Custom renderer for toolstrip checkbox color.
    /// </summary>
    public class CheckBoxRenderer : ToolStripSystemRenderer
    {
        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            var btn = e.Item as ToolStripButton;

            if (!(btn is null) && btn.CheckOnClick && btn.Checked)
            {
                Rectangle bounds = new Rectangle(Point.Empty, e.Item.Size);
                e.Graphics.FillRectangle(new SolidBrush(UserSettings.TheSettings.SelectedColor), bounds);
            }
            else
            {
                base.OnRenderButtonBackground(e);
            }
        }
    }
}
