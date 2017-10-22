using System;
using System.Drawing;
using System.Windows.Forms;
using Nebulator.Common;

namespace Nebulator.Controls
{
    /// <summary>
    /// Control potentiometer.
    /// </summary>
    public partial class Pot : UserControl
    {
        #region Fields
        double _minimum = 0.0;
        double _maximum = 1.0;
        double _value = 0.5;
        int _beginDragY = 0;
        double _beginDragValue = 0.0;
        bool _dragging = false;
        #endregion

        #region Properties
        /// <summary>
        /// For styling.
        /// </summary>
        public Color ControlColor { get; set; } = Color.Black;

        /// <summary>
        /// Minimum Value of the Pot.
        /// </summary>
        public double Minimum
        {
            get { return _minimum; }
            set { _minimum = Math.Min(value, _maximum); }
        }

        /// <summary>
        /// Maximum Value of the Pot.
        /// </summary>
        public double Maximum
        {
            get { return _maximum; }
            set { _maximum = Math.Max(value, _minimum); }
        }

        /// <summary>
        /// The current value of the pot.
        /// </summary>
        public double Value
        {
            get { return _value; }
            set { SetValue(value, false); }
        }
        #endregion

        /// <summary>
        /// Value changed event.
        /// </summary>
        public event EventHandler ValueChanged;

        /// <summary>
        /// Creates a new pot control.
        /// </summary>
        public Pot()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            InitializeComponent();
        }

        /// <summary>
        /// Set the new value.
        /// </summary>
        /// <param name="newValue"></param>
        /// <param name="raiseEvents"></param>
        private void SetValue(double newValue, bool raiseEvents)
        {
            _value = Utils.Constrain(newValue, _minimum, _maximum);

            if (raiseEvents)
            {
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
            Invalidate();
        }

        /// <summary>
        /// Draws the control.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            int diameter = Math.Min(Width - 4, Height - 4);

            Pen potPen = new Pen(ControlColor, 3.0f)
            {
                LineJoin = System.Drawing.Drawing2D.LineJoin.Round
            };
            System.Drawing.Drawing2D.GraphicsState state = e.Graphics.Save();
            e.Graphics.TranslateTransform(Width / 2, Height / 2);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.DrawArc(potPen, new Rectangle(diameter / -2, diameter / -2, diameter, diameter), 135, 270);

            double percent = (_value - _minimum) / (_maximum - _minimum);
            double degrees = 135 + (percent * 270);
            double x = (diameter / 2.0) * Math.Cos(Math.PI * degrees / 180);
            double y = (diameter / 2.0) * Math.Sin(Math.PI * degrees / 180);
            e.Graphics.DrawLine(potPen, 0, 0, (float)x, (float)y);

            StringFormat format = new StringFormat()
            {
                LineAlignment = StringAlignment.Center,
                Alignment = StringAlignment.Center
            };
            string sValue = $"{_value:F2}";
            Rectangle srect = new Rectangle(0, 10, 0, 0);
            e.Graphics.DrawString(sValue, Font, Brushes.Black, srect, format);

            e.Graphics.Restore(state);
            base.OnPaint(e);
        }

        /// <summary>
        /// Handles the mouse down event to allow changing value by dragging.
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            _dragging = true;
            _beginDragY = e.Y;
            _beginDragValue = _value;
            base.OnMouseDown(e);
        }

        /// <summary>
        /// Handles the mouse up event to allow changing value by dragging.
        /// </summary>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            _dragging = false;
            base.OnMouseUp(e);
        }

        /// <summary>
        /// Handles the mouse down event to allow changing value by dragging.
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_dragging)
            {
                int yDifference = _beginDragY - e.Y;
                double delta = (_maximum - _minimum) * (yDifference / 100.0);
                double newValue = Utils.Constrain(_beginDragValue + delta, _minimum, _maximum);
                SetValue(newValue, true);
            }
            base.OnMouseMove(e);
        }
    }
}
