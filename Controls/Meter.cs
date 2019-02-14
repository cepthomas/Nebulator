using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Nebulator.Common;


namespace Nebulator.Controls
{
    public enum MeterType { Linear, Log, Continuous };

    /// <summary>
    /// Implements a rudimentary volume meter.
    /// </summary>
    public partial class Meter : UserControl //TODON1 add to UI
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
        /// For styling.
        /// </summary>
        public Color ControlColor { get; set; } = Color.Orange;

        /// <summary>
        /// How the meter responds.
        /// </summary>
        public MeterType MeterType { get; set; } = MeterType.Log;

        /// <summary>
        /// Minimum value. If Log type, this is in db.
        /// </summary>
        public double MinValue { get; set; } = -60;

        /// <summary>
        /// Maximum value. If Log type, this is in db.
        /// </summary>
        public double MaxValue { get; set; } = 18;

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
        /// Add a new data point.
        /// </summary>
        /// <param name="val"></param>
        public void AddValue(double val)
        {
            // Sometimes when you minimise, max samples can be set to 0.
            if (_maxBuff != 0)
            {
                switch (MeterType)
                {
                    case MeterType.Log:
                        double db = Utils.Constrain(20 * Math.Log10(val), MinValue, MaxValue);
                        _buff[0] = db;
                        break;

                    case MeterType.Linear:
                        double lval = Utils.Constrain(val, MinValue, MaxValue);
                        _buff[0] = lval;
                        break;

                    case MeterType.Continuous:
                        double lvalc = Utils.Constrain(val, MinValue, MaxValue);

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
                    percent = (_buff[0] - MinValue) / (MaxValue - MinValue);

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

            //base.OnPaint(pe);
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
