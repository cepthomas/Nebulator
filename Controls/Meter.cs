using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Nebulator.Common;


namespace Nebulator.Controls
{
    /// <summary>Display types.</summary>
    public enum MeterType { Linear, Log, Continuous };

    /// <summary>
    /// Implements a rudimentary volume meter.
    /// </summary>
    public partial class Meter : UserControl
    {
        #region Fields
        /// <summary>
        /// Storage.
        /// </summary>
        List<double> _buff = new List<double>(1000);

        /// <summary>
        /// Storage.
        /// </summary>
        int _maxBuff;

        /// <summary>
        /// Storage.
        /// </summary>
        int _buffIndex;
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
        /// How the meter responds.
        /// </summary>
        public MeterType MeterType { get; set; } = MeterType.Linear;

        /// <summary>
        /// Minimum value. If Log type, this is in db - usually -60;
        /// </summary>
        public double Minimum { get; set; } = 0;

        /// <summary>
        /// Maximum value. If Log type, this is in db - usually +18.
        /// </summary>
        public double Maximum { get; set; } = 100;

        /// <summary>
        /// Meter orientation.
        /// </summary>
        public Orientation Orientation { get; set; } = Orientation.Horizontal;
        #endregion

        #region Public functions
        /// <summary>
        /// Basic volume meter.
        /// </summary>
        public Meter()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            InitializeComponent();
        }

        /// <summary>
        /// Add a new data point. If Log, this will convert for you.
        /// </summary>
        /// <param name="val"></param>
        public void AddValue(double val)
        {
            // Sometimes when you minimize, max samples can be set to 0.
            if (_maxBuff != 0)
            {
                switch (MeterType)
                {
                    case MeterType.Log:
                        double db = Utils.Constrain(20 * Math.Log10(val), Minimum, Maximum);
                        _buff[0] = db;
                        break;

                    case MeterType.Linear:
                        double lval = Utils.Constrain(val, Minimum, Maximum);
                        _buff[0] = lval;
                        break;

                    case MeterType.Continuous:
                        double lvalc = Utils.Constrain(val, Minimum, Maximum);

                        if (_buff.Count <= _maxBuff)
                        {
                            _buff.Add(lvalc);
                        }
                        else if (_buffIndex < _maxBuff)
                        {
                            _buff[_buffIndex] = lvalc;
                        }

                        _buffIndex++;
                        _buffIndex %= _maxBuff;
                        break;
                }

                Invalidate();
            }
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Paints the volume meter.
        /// </summary>
        protected override void OnPaint(PaintEventArgs pe)
        {
            double percent = 0;
            pe.Graphics.DrawRectangle(Pens.Black, 0, 0, Width - 1, Height - 1);
            int w = Width - 2;
            int h = Height - 2;
            Brush brush = new SolidBrush(ControlColor);
            Pen pen = new Pen(ControlColor);

            switch (MeterType)
            {
                case MeterType.Log:
                case MeterType.Linear:
                    percent = _buff.Count > 0 ? (_buff[0] - Minimum) / (Maximum - Minimum) : 0;

                    if (Orientation == Orientation.Horizontal)
                    {
                        w = (int)(w * percent);
                        pe.Graphics.FillRectangle(brush, 1, 1, w, h);
                    }
                    else
                    {
                        h = (int)(h * percent);
                        pe.Graphics.FillRectangle(brush, 1, Height - 1 - h, w, h);
                    }
                    break;

                case MeterType.Continuous:
                    for (int x = 0; x < Width; x++)
                    {
                        int index = x - Width + _buffIndex;
                        double val = (index >= 0 & index < _buff.Count) ? _buff[index] : 0;
                        float lineHeight = Height * (float)val;
                        float y1 = (Height - lineHeight) / 2;
                        pe.Graphics.DrawLine(pen, x, y1, x, y1 + lineHeight);
                    }
                    break;
            }

            if (Label != "" && Orientation == Orientation.Horizontal)
            {
                StringFormat format = new StringFormat()
                {
                    LineAlignment = StringAlignment.Center,
                    Alignment = StringAlignment.Center
                };

                Rectangle r = new Rectangle(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height / 2);
                pe.Graphics.DrawString(Label, Font, Brushes.Black, r, format);
            }
        }

        /// <summary>
        /// Update drawing area.
        /// </summary>
        protected override void OnResize(EventArgs e)
        {
            _maxBuff = Width;
            base.OnResize(e);
        }
        #endregion
    }
}
