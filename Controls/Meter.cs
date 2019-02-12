using System;
using System.Drawing;
using System.Windows.Forms;
using Nebulator.Common;

namespace Nebulator.Controls
{
    public enum MeterType { Linear, Log };

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
        /// How the meter responds.
        /// </summary>
        public MeterType MeterType { get; set; } = MeterType.Log;

        /// <summary>
        /// Minimum value. If Log type, this is db.
        /// </summary>
        public double MinValue { get; set; } = -60;

        /// <summary>
        /// Maximum value. If Log type, this is db.
        /// </summary>
        public double MaxValue { get; set; } = 18;

        /// <summary>
        /// Meter orientation.
        /// </summary>
        public Orientation Orientation { get; set; } = Orientation.Horizontal;
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
            double percent = 0;

            switch (MeterType)
            {
                case MeterType.Log:
                    double db = Utils.Constrain(20 * Math.Log10(Value), MinValue, MaxValue);
                    percent = (db - MinValue) / (MaxValue - MinValue);
                    break;

                case MeterType.Linear:
                    double lval = Utils.Constrain(Value, MinValue, MaxValue);
                    percent = (lval - MinValue) / (MaxValue - MinValue);
                    break;
            }

            pe.Graphics.DrawRectangle(Pens.Black, 0, 0, Width - 1, Height - 1);

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
