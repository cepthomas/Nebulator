using System;
using System.Windows.Forms;
using System.Drawing;

namespace Nebulator.Controls
{
    /// <summary>
    /// Summary description for Fader.
    /// </summary>
    public partial class Fader : UserControl
    {
        #region Fields
        double _value = 0;
        bool _dragging;
        int _dragy;
        const int SLIDER_HEIGHT = 30;
        const int SLIDER_WIDTH = 15;
        Rectangle _rect;
        #endregion

        #region Properties
        /// <summary>
        /// Minimum value of this fader
        /// </summary>
        public double Minimum { get; set; }

        /// <summary>
        /// Maximum value of this fader
        /// </summary>
        public double Maximum { get; set; }

        /// <summary>
        /// Current value of this fader
        /// </summary>
        public double Value
        {
            get { return (_value * (Maximum - Minimum)) + Minimum; }
            set { _value = (value - Minimum) / (Maximum - Minimum); }
        }

        /// <summary>
        /// Fader orientation
        /// </summary>
        public Orientation Orientation { get; set; }
        #endregion

        /// <summary>
        /// Creates a new Fader control.
        /// </summary>
        public Fader()
        {
            InitializeComponent();
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }

        /// <summary>
        /// </summary>
        void DrawSlider(Graphics g)
        {
            Brush block = new SolidBrush(Color.White);
            Pen centreLine = new Pen(Color.Black);

            _rect.X = (Width - SLIDER_WIDTH) / 2;
            _rect.Width = SLIDER_WIDTH;
            _rect.Y = (int)((Height - SLIDER_HEIGHT) * _value);
            _rect.Height = SLIDER_HEIGHT;

            g.FillRectangle(block, _rect);
            g.DrawLine(centreLine, _rect.Left, _rect.Top + _rect.Height / 2, _rect.Right, _rect.Top + _rect.Height / 2);
            block.Dispose();
            centreLine.Dispose();

            //g.DrawImage(Images.Fader1,sliderRectangle);
        }

        /// <summary>
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            if (Orientation == Orientation.Vertical)
            {
                Brush groove = new SolidBrush(Color.Black);
                g.FillRectangle(groove, Width / 2, SLIDER_HEIGHT / 2, 2, Height - SLIDER_HEIGHT);
                groove.Dispose();
                DrawSlider(g);
            }

            base.OnPaint(e);
        }

        /// <summary>
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (_rect.Contains(e.X, e.Y))
            {
                _dragging = true;
                _dragy = e.Y - _rect.Y;
            }
            base.OnMouseDown(e);
        }

        /// <summary>
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_dragging)
            {
                int sliderTop = e.Y - _dragy;
                if (sliderTop < 0)
                {
                    _value = 0;
                }
                else if (sliderTop > Height - SLIDER_HEIGHT)
                {
                    _value = 1;
                }
                else
                {
                    _value = (double)sliderTop / (Height - SLIDER_HEIGHT);
                }
                //Console.WriteLine(_value);
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        /// <summary>
        /// </summary>        
        protected override void OnMouseUp(MouseEventArgs e)
        {
            _dragging = false;
            base.OnMouseUp(e);
        }
    }
}