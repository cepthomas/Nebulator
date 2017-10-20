using System;
using System.Drawing;
using System.Windows.Forms;
using Nebulator.Common;

namespace Nebulator.Controls
{
    /// <summary>
    /// Pan slider control
    /// </summary>
    public partial class Pan : UserControl
    {
        private double _value;

        /// <summary>
        /// The current Pan setting.
        /// </summary>
        public double Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = Utils.Constrain(value, -1.0, 1.0);
                PanChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        /// <summary>
        /// For styling.
        /// </summary>
        public Color ControlColor { get; set; } = Color.Orange;

        /// <summary>
        /// True when pan value changed.
        /// </summary>
        public event EventHandler PanChanged;

        /// <summary>
        /// Creates a new PanSlider control.
        /// </summary>
        public Pan()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();
        }

        /// <summary>
        /// Draw control.
        /// </summary>
        protected override void OnPaint(PaintEventArgs pe)
        {
            StringFormat format = new StringFormat()
            {
                LineAlignment = StringAlignment.Center,
                Alignment = StringAlignment.Center
            };
            string panValue;
            Brush brush = new SolidBrush(ControlColor);

            if (_value == 0.0)
            {
                pe.Graphics.FillRectangle(brush, (Width / 2) - 1, 1, 3, Height - 2);
                panValue = "C";
            }
            else if (_value > 0)
            {
                pe.Graphics.FillRectangle(brush, (Width / 2), 1, (int)((Width / 2) * _value), Height - 2);
                panValue = $"{_value * 100:F0}%R";
            }
            else
            {
                pe.Graphics.FillRectangle(brush, (int)((Width / 2) * (_value + 1)), 1, (int)((Width / 2) * (0 - _value)), Height - 2);
                panValue = $"{_value * -100:F0}%L";
            }
            pe.Graphics.DrawRectangle(Pens.Black, 0, 0, Width - 1, Height - 1);

            pe.Graphics.DrawString(panValue, Font, Brushes.Black, ClientRectangle, format);

            //base.OnPaint(pe);
        }

        /// <summary>
        /// Handle dragging.
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                SetValuePanFromMouse(e.X);
            }
            base.OnMouseMove(e);
        }

        /// <summary>
        /// Handle dragging.
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            SetValuePanFromMouse(e.X);
            base.OnMouseDown(e);
        }

        /// <summary>
        /// Calculate position.
        /// </summary>
        /// <param name="x"></param>
        private void SetValuePanFromMouse(int x)
        {
            Value = (((double)x / Width) * 2.0f) - 1.0f;
        }
    }
}
