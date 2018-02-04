using System;
using System.Drawing;
using System.Windows.Forms;
using Nebulator.Common;

namespace Nebulator.Controls
{
    /// <summary>
    /// Implements a rudimentary volume meter.
    /// </summary>
    public partial class Meter : UserControl
    {
        #region Fields
        private double _value = 0;
        #endregion

        #region Properties
        /// <summary>
        /// For styling.
        /// </summary>
        public Color ControlColor { get; set; } = Color.Orange;

        /// <summary>
        /// Amplitude property.
        /// </summary>
        public double Value
        {
            get { return _value; }
            set { _value = value; Invalidate(); }
        }

        /// <summary>
        /// Minimum decibels.
        /// </summary>
        public double MinDb { get; set; } = -60;

        /// <summary>
        /// Maximum decibels.
        /// </summary>
        public double MaxDb { get; set; } = 18;

        /// <summary>
        /// Meter orientation.
        /// </summary>
        public Orientation Orientation { get; set; } = Orientation.Vertical;
        #endregion

        /// <summary>
        /// Basic volume meter.
        /// </summary>
        public Meter()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            InitializeComponent();
        }

        /// <summary>
        /// Paints the volume meter.
        /// </summary>
        protected override void OnPaint(PaintEventArgs pe)
        {
            pe.Graphics.DrawRectangle(Pens.Black, 0, 0, Width - 1, Height - 1);

            double db = Utils.Constrain(20 * Math.Log10(Value), MinDb, MaxDb);
            double percent = (db - MinDb) / (MaxDb - MinDb);

            int width = Width - 2;
            int height = Height - 2;
            Brush brush = new SolidBrush(ControlColor);
            if (Orientation == Orientation.Horizontal)
            {
                width = (int)(width * percent);
                pe.Graphics.FillRectangle(brush, 1, 1, width, height);
            }
            else
            {
                height = (int)(height * percent);
                pe.Graphics.FillRectangle(brush, 1, Height - 1 - height, width, height);
            }
            //base.OnPaint(pe);
        }
    }
}
