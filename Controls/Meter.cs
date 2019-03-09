using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Nebulator.Common;


namespace Nebulator.Controls
{
    /// <summary>Display types.</summary>
    public enum MeterType { Linear, Log, ContinuousLine, ContinuousDots };

    /// <summary>
    /// Implements a rudimentary volume meter.
    /// </summary>
    public partial class Meter : UserControl
    {
        #region Fields
        /// <summary>
        /// Storage.
        /// </summary>
        //List<double> _buff = null;
        double[] _buff = { };

        /// <summary>
        /// Storage.
        /// </summary>
        int _buffIndex = 0;

        /// <summary>
        /// A number.
        /// </summary>
        const int BORDER_WIDTH = 1;
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
        /// Init stuff.
        /// </summary>
        private void Meter_Load(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Add a new data point. If Log, this will convert for you.
        /// </summary>
        /// <param name="val"></param>
        public void AddValue(double val)
        {
            // Sometimes when you minimize, samples can be set to 0.
            if (_buff.Length != 0)
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

                    case MeterType.ContinuousLine:
                    case MeterType.ContinuousDots:
                        // Bump ring index.
                        _buffIndex++;
                        _buffIndex %= _buff.Length;
                        _buff[_buffIndex] = Utils.Constrain(val, Minimum, Maximum);
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
            // Setup.
            pe.Graphics.Clear(UserSettings.TheSettings.BackColor);
            Brush brush = new SolidBrush(ControlColor);
            Pen pen = new Pen(ControlColor);

            // Draw border.
            int bw = BORDER_WIDTH;
            Pen penBorder = new Pen(Color.Black, bw);
            pe.Graphics.DrawRectangle(penBorder, 0, 0, Width - 1, Height - 1);

            // Draw data.
            Rectangle drawArea = Rectangle.Inflate(ClientRectangle, -bw, -bw);

            switch (MeterType)
            {
                case MeterType.Log:
                case MeterType.Linear:
                    double percent = _buff.Length > 0 ? (_buff[0] - Minimum) / (Maximum - Minimum) : 0;

                    if (Orientation == Orientation.Horizontal)
                    {
                        int w = (int)(drawArea.Width * percent);
                        int h = drawArea.Height;
                        pe.Graphics.FillRectangle(brush, bw, bw, w, h);
                    }
                    else
                    {
                        int w = drawArea.Width;
                        int h = (int)(drawArea.Height * percent);
                        pe.Graphics.FillRectangle(brush, bw, Height - bw - h, w, h);
                    }
                    break;

                case MeterType.ContinuousLine:
                case MeterType.ContinuousDots:
                    for (int i = 0; i < _buff.Length; i++)
                    {
                        int index = _buffIndex - i;
                        index = index < 0 ? index + _buff.Length : index;

                        double val = _buff[index];

                        // Draw data point.
                        double x = i + bw;
                        double y = Utils.Map(val, Minimum, Maximum, Height - 2 * bw, bw);

                        if(MeterType == MeterType.ContinuousLine)
                        {
                            pe.Graphics.DrawLine(pen, (float)x, (float)y, (float)x, Height - 2 * bw);
                        }
                        else
                        {
                            pe.Graphics.FillRectangle(brush, (float)x, (float)y, 2, 2);
                        }
                    }
                    break;
            }

            if (Label.Length > 0 && Orientation == Orientation.Horizontal)
            {
                StringFormat format = new StringFormat() { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center };

                Rectangle r = new Rectangle(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height / 2);
                pe.Graphics.DrawString(Label, Font, Brushes.Black, r, format);
            }
        }

        /// <summary>
        /// Update drawing area.
        /// </summary>
        protected override void OnResize(EventArgs e)
        {
            _buff = new double[Width - 2 * BORDER_WIDTH];
            _buffIndex = 0;
            base.OnResize(e);
            Invalidate();
        }
        #endregion
    }
}
