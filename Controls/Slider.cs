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
        double _value = 0.0;
        double _resetVal = 0.0;
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
        public double Maximum { get; set; } = 1.0;

        /// <summary>
        /// Minimum value of this slider.
        /// </summary>
        public double Minimum { get; set; } = 0.0;

        /// <summary>
        /// Reset value of this slider.
        /// </summary>
        public double ResetValue
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
        public double Value
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

        /// <summary>
        /// Number of decimal places to display.
        /// </summary>
        public int DecPlaces { get; set; } = 1;
        #endregion

        #region Events
        /// <summary>
        /// Slider value changed event.
        /// </summary>
        public event EventHandler ValueChanged;
        #endregion

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
            // Setup.
            pe.Graphics.Clear(UserSettings.TheSettings.BackColor);
            Brush brush = new SolidBrush(ControlColor);
            Pen pen = new Pen(ControlColor);

            // Draw border.
            int bw = Utils.BORDER_WIDTH;
            Pen penBorder = new Pen(Color.Black, bw);
            pe.Graphics.DrawRectangle(penBorder, 0, 0, Width - 1, Height - 1);

            // Draw data.
            Rectangle drawArea = Rectangle.Inflate(ClientRectangle, -bw, -bw);

            double x = Width * (_value - Minimum) / (Maximum - Minimum);
            pe.Graphics.FillRectangle(brush, bw, bw, (float)x - 2 * bw, Height - 2 * bw);

            // Text.
            StringFormat format = new StringFormat() { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center };
            string sval = _value.ToString("#0." + new string('0', DecPlaces));

            if (Label != "")
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
            double newval = Minimum + x * (Maximum - Minimum) / Width;
            Value = Utils.Constrain(newval, Minimum, Maximum);
        }
    }
}
