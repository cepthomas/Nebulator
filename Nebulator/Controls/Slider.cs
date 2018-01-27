using System;
using System.Drawing;
using System.Windows.Forms;
using Nebulator.Common;


namespace Nebulator.Controls
{
    /// <summary>
    /// Slider control.
    /// </summary>
    public partial class Slider : UserControl
    {
        #region Fields
        int _value = 50;
        int _resetVal = 0;
        #endregion

        #region Properties
        /// <summary>
        /// Optional label.
        /// </summary>
        public string Label { get; set; } = "";

        /// <summary>
        /// For styling.
        /// </summary>
        public Color ControlColor { get; set; } = Color.Orange;

        /// <summary>
        /// Maximum value of this slider.
        /// </summary>
        public int Maximum { get; set; } = 100;

        /// <summary>
        /// Minimum value of this slider.
        /// </summary>
        public int Minimum { get; set; } = 0;

        /// <summary>
        /// Reset value of this slider.
        /// </summary>
        public int ResetValue
        {
            get
            {
                return _resetVal;
            }
            set
            {
                _resetVal = Utils.Constrain(value, Minimum, Maximum);
            }
        }

        /// <summary>
        /// The value for this slider.
        /// </summary>
        public int Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = Utils.Constrain(value, Minimum, Maximum);
                ValueChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }
        #endregion

        /// <summary>
        /// Slider value changed event.
        /// </summary>
        public event EventHandler ValueChanged;

        /// <summary>
        /// Creates a new Slider control.
        /// </summary>
        public Slider()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            InitializeComponent();
        }

        /// <summary>
        /// Draw the slider.
        /// </summary>
        protected override void OnPaint(PaintEventArgs pe)
        {
            // Outline.
            pe.Graphics.DrawRectangle(Pens.Black, 0, 0, Width - 1, Height - 1);

            // Internal.
            Brush brush = new SolidBrush(ControlColor);
            int x = Width * (_value - Minimum) / (Maximum - Minimum);
            pe.Graphics.FillRectangle(brush, 1, 1, x - 2, Height - 2);

            // Text.
            StringFormat format = new StringFormat()
            {
                LineAlignment = StringAlignment.Center,
                Alignment = StringAlignment.Center
            };
            string sval = $"{_value}";

            if(Label != "")
            {
                Rectangle r = new Rectangle(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height / 2);
                pe.Graphics.DrawString(Label, Font, Brushes.Black, r, format);
                r = new Rectangle(ClientRectangle.X, ClientRectangle.Height / 2, ClientRectangle.Width, ClientRectangle.Height / 2);
                pe.Graphics.DrawString(sval, Font, Brushes.Black, r, format);
            }
            else
            {
                pe.Graphics.DrawString(sval, Font, Brushes.Black, ClientRectangle, format);
            }
        }

        /// <summary>
        /// Handle dragging.
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                SetValueFromMouse(e.X);
            }
            base.OnMouseMove(e);
        }

        /// <summary>
        /// Handle dragging.
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            switch(e.Button)
            {
                case MouseButtons.Left:
                    SetValueFromMouse(e.X);
                    break;

                case MouseButtons.Right:
                    Value = ResetValue;
                    break;
            }

            base.OnMouseDown(e);
        }

        /// <summary>
        /// ommon updater.
        /// </summary>
        /// <param name="x"></param>
        private void SetValueFromMouse(int x)
        {
            int newval = Minimum + x * (Maximum - Minimum) / Width;
            Value = Utils.Constrain(newval, Minimum, Maximum);
        }
    }
}
