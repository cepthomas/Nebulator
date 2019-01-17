using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SkiaSharp;
using SkiaSharp.Views.Desktop;


// TODON2 add live/oscope mode.
// TODON2 Separate X/Y zooms. Also centering is not quite right.


namespace Nebulator.Visualizer
{
    /// <summary>Supported chart types.</summary>
    public enum ChartType { Line, Scatter, ScatterLine };

    /// <summary>
    /// A simple display of numerical series.
    /// </summary>
    public partial class VisualizerForm : Form
    {
        #region Properties for client customization
        ///<summary>Data to chart.</summary>
        public List<DataSeries> AllSeries { get; set; } = new List<DataSeries>();

        ///<summary></summary>
        public ChartType ChartType { get; set; } = ChartType.ScatterLine;

        ///<summary></summary>
        public double DotSize { get; set; } = 3;

        ///<summary></summary>
        public double LineSize { get; set; } = 1;

        ///<summary></summary>
        public string XUnits { get; set; } = "X units";

        ///<summary></summary>
        public string YUnits { get; set; } = "Y units";
        #endregion

        #region Fields
        ///<summary>Current zoom ratio. 1 is all the way out aka home.</summary>
        double _zoom = 1;

        ///<summary>How much to zoom by.</summary>
        const double ZOOM_INC = 0.2;

        ///<summary>Data range.</summary>
        double _xMin = double.MaxValue;

        ///<summary>Data range.</summary>
        double _xMax = double.MinValue;

        ///<summary>Data range.</summary>
        double _yMin = double.MaxValue;

        ///<summary>Data range.</summary>
        double _yMax = double.MinValue;

        ///<summary>Mouse tracking.</summary>
        PointF _lastMousePos = new PointF();

        ///<summary>Mouse tracking.</summary>
        PointF _currentMousePos = new PointF();
        #endregion

        #region Geometry
        /// <summary>Whitespace around edges.</summary>
        const int BORDER_PAD = 20;

        /// <summary>Reserved for axes.</summary>
        const int AXIS_SPACE = 40;

        /// <summary>UI region to draw the dots.</summary>
        RectangleF _dataRegion = new RectangleF();

        /// <summary>Displayed data is this far away from actual center.</summary>
        PointF _dataOffset = new PointF();
        #endregion

        #region Coloring
        /// <summary>Qualitative color set from http://colorbrewer2.org.</summary>
        List<Color> _colors1 = new List<Color>()
        {
            Color.FromArgb(217, 95, 2), Color.FromArgb(27, 158, 119),
            Color.FromArgb(117, 112, 179), Color.FromArgb(231, 41, 138),
            Color.FromArgb(102, 166, 30), Color.FromArgb(230, 171, 2),
            Color.FromArgb(166, 118, 29), Color.FromArgb(102, 102, 102)
        };

        /// <summary>Qualitative color set from http://colorbrewer2.org.</summary>
        List<Color> _colors2 = new List<Color>()
        {
            Color.FromArgb(228, 26, 28), Color.FromArgb(55, 126, 184),
            Color.FromArgb(77, 175, 74), Color.FromArgb(152, 78, 163),
            Color.FromArgb(255, 127, 0), Color.FromArgb(255, 255, 51),
            Color.FromArgb(166, 86, 40), Color.FromArgb(247, 129, 191)
        };

        /// <summary>Named colors.</summary>
        List<Color> _colors3 = new List<Color>()
        {
           Color.Firebrick, Color.CornflowerBlue, Color.MediumSeaGreen, Color.MediumOrchid,
           Color.DarkOrange, Color.DarkGoldenrod, Color.DarkSlateGray, Color.Khaki, Color.PaleVioletRed
        };

        /// <summary>Color set in use.</summary>
        List<Color> _colors = new List<Color>();
        static int _colorIndex = 0;
        #endregion

        #region Drawing tools
        /// <summary>Current pen to draw with.</summary>
        SKPaint _pen = new SKPaint()
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            IsStroke = true,
            StrokeWidth = 2,
            FilterQuality = SKFilterQuality.High,
            IsAntialias = true
        };

        /// <summary>Current brush to draw with.</summary>
        SKPaint _fill = new SKPaint()
        {
            Color = SKColors.Yellow,
            Style = SKPaintStyle.Fill,
            IsStroke = false,
            FilterQuality = SKFilterQuality.High,
            IsAntialias = true
        };

        /// <summary>Current font to draw with.</summary>
        SKPaint _text = new SKPaint()
        {
            TextSize = 14,
            Color = SKColors.Black,
            Typeface = SKTypeface.FromFamilyName("Arial"),
            TextAlign = SKTextAlign.Left,
            IsAntialias = true
        };
        #endregion

        #region Lifecycle
        /// <summary>
        /// Default constructor.
        /// </summary>
        public VisualizerForm()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            UpdateStyles();

            // Improve performance and eliminate flicker.
            DoubleBuffered = true;
        }

        void VisualizerForm_Load(object sender, EventArgs e)
        {
            // Get the bitmap. Convert to an icon and use for the form's icon.
            Bitmap bm = new Bitmap(Properties.Resources.glyphicons_41_stats);
            Icon = Icon.FromHandle(bm.GetHicon());

            // Intercept all keyboard events.
            KeyPreview = true;

            // Colors.
            _colors.AddRange(_colors1);
            _colors.AddRange(_colors2);
            //_colorIndex = new Random().Next(0, _colors.Count);

            // Hook up handlers.
            KeyPress += VisualizerForm_KeyPress;
            MouseWheel += VisualizerForm_MouseWheel;
            skControl.Resize += SkControl_Resize;
            skControl.MouseUp += SkControl_MouseUp;
            skControl.MouseMove += SkControl_MouseMove;
            skControl.MouseClick += SkControl_MouseClick;
            skControl.PaintSurface += SkControl_PaintSurface;

            // Assumes user has populated series.
            InitData();
        }
        #endregion

        #region Mouse Event Handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SkControl_MouseClick(object sender, MouseEventArgs e)
        {
            // if (e.Button == MouseButtons.Right)
            // {
            //     ContextMenuStrip cms = new ContextMenuStrip();
            //     cms.ShowImageMargin = false;
            //     cms.Items.Add("Set Scale", null, new EventHandler(ShowChartScale));
            //     cms.Show(this, new Point(e.X, e.Y));
            // }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SkControl_MouseUp(object sender, MouseEventArgs e)
        {
            PointF newPos = new PointF(e.X, e.Y);

            if (e.Button == MouseButtons.Right)
            {
                // TODON2 Set chart to selection area. Must update zoom values too.
                Repaint();
            }
            else // Left
            {
                // Finished dragging.
                Repaint();
            }
        }

        /// <summary>
        /// If the _mouseDown state is true then move the chart with the mouse.
        /// If the mouse is over a data point, show its coordinates.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SkControl_MouseMove(object sender, MouseEventArgs e)
        {
            _currentMousePos = new PointF(e.X, e.Y);

            if(_lastMousePos != _currentMousePos) // Apparently a known issue is that showing a tooltip causes a MouseMove event to get generated.
            {
                if (e.Button.HasFlag(MouseButtons.Left)) // move the chart
                {
                    float xChange = _currentMousePos.X - _lastMousePos.X;
                    float yChange = _currentMousePos.Y - _lastMousePos.Y;

                    // If there is a change in x or y...
                    if (xChange != 0 || yChange != 0)
                    {
                        _dataOffset.Y += yChange;
                        _dataOffset.X += xChange;
                        Repaint();
                    }
                }
                else if (e.Button.HasFlag(MouseButtons.Right)) // draw selection region TODON2
                {
                    Repaint();
                }
                else // tooltip?
                {
                    DataPoint closestPoint = GetClosestPoint(_currentMousePos);

                    if (closestPoint != null)
                    {
                        // Display the tooltip
                        toolTip.Show(closestPoint.ToString(), skControl, (int)(_currentMousePos.X + 15), (int)(_currentMousePos.Y));
                    }
                    else
                    {
                        // Hide the tooltip
                        toolTip.Hide(this);
                    }
                }

                _lastMousePos = _currentMousePos;
            }
        }

        /// <summary>Zooms in or out depending on the direction the mouse wheel is moved.</summary>
        /// <param name="sender">Object that sent triggered the event.</param>
        /// <param name="e">The particular MouseEventArgs (DoubleClick, Click, etc.).</param>
        private void VisualizerForm_MouseWheel(object sender, MouseEventArgs e)
        {
            HandledMouseEventArgs hme = (HandledMouseEventArgs)e;
            hme.Handled = true;

            if (hme.X <= Width && hme.Y <= Height)
            {
                Zoom(hme.Delta > 0 ? ZOOM_INC : -ZOOM_INC);
            }
        }
        #endregion

        #region Key Event Handlers
        /// <summary>
        /// Top level key press handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VisualizerForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            switch (char.ToUpper(e.KeyChar))
            {
                case '+':
                case '=':
                    Zoom(ZOOM_INC);
                    break;

                case '-':
                case '_':
                    Zoom(-ZOOM_INC);
                    break;

                case 'h':
                case 'H':
                    _dataOffset = new PointF();
                    Zoom(0);
                    break;
            }
        }
        #endregion

        #region Window Event Handlers
        private void SkControl_Resize(object sender, EventArgs e)
        {
            CalcGeometry();
            Repaint();
        }
        #endregion

        #region Render functions
        /// <summary>
        /// Draw the main display area.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SkControl_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            SKCanvas canvas = e.Surface.Canvas;
            canvas.Clear();

            // Draw axes first before clipping.
            DrawAxes(canvas);

            // Now clip to drawing region.
            canvas.ClipRect(_dataRegion.ToSKRect());

            TransformData();

            DrawData(canvas);
        }

        /// <summary>
        /// Map the data to UI space.
        /// </summary>
        void TransformData()
        {
            // https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/graphics/skiasharp/transforms/matrix
            SKMatrix matrix = new SKMatrix();
            matrix.ScaleX = (float)(_zoom * (_dataRegion.Right - _dataRegion.Left) / (_xMax - _xMin));
            matrix.ScaleY = (float)(_zoom * (_dataRegion.Top - _dataRegion.Bottom) / (_yMax - _yMin));
            matrix.TransX = _dataOffset.X + _dataRegion.Left;
            matrix.TransY = _dataOffset.Y + _dataRegion.Bottom;
            matrix.Persp2 = 1;

            foreach (DataSeries ser in AllSeries)
            {
                foreach(DataPoint dp in ser.Points)
                {
                    dp.ClientPoint = matrix.MapPoint(new SKPoint((float)(dp.X - _xMin), (float)(dp.Y - _yMin)));
                }
            }
        }

        /// <summary>
        /// Draw line/scatter charts.
        /// </summary>
        /// <param name="canvas"></param>
        void DrawData(SKCanvas canvas)
        {
            foreach (DataSeries ser in AllSeries)
            {
                _pen.Color = ser.Color.ToSKColor();

                if (ChartType == ChartType.Scatter || ChartType == ChartType.ScatterLine)
                {
                    _pen.StrokeWidth = (float)DotSize;
                    ser.Points.ForEach(pt => canvas.DrawPoint(pt.ClientPoint, _pen));
                }

                if (ChartType == ChartType.Line || ChartType == ChartType.ScatterLine)
                {
                    _pen.StrokeWidth = (float)LineSize;
                    SKPath path = new SKPath();
                    SKPoint[] points = new SKPoint[ser.Points.Count];
                    for (int i = 0; i < ser.Points.Count; i++)
                    {
                        points[i] = ser.Points[i].ClientPoint;
                    }
                    
                    path.AddPoly(points, false);
                    canvas.DrawPath(path, _pen);
                }
            }
        }

        /// <summary>
        /// Draw axes.
        /// </summary>
        /// <param name="canvas"></param>
        void DrawAxes(SKCanvas canvas)
        {
            _pen.Color = SKColors.Black;
            _pen.StrokeWidth = 0.6f;
            _text.Color = SKColors.Black;

            // Draw area.
            float tick = 8;
            canvas.DrawLine(_dataRegion.Left - tick, _dataRegion.Top, _dataRegion.Right, _dataRegion.Top, _pen);
            canvas.DrawLine(_dataRegion.Left - tick, _dataRegion.Bottom, _dataRegion.Right, _dataRegion.Bottom, _pen);
            canvas.DrawLine(_dataRegion.Left, _dataRegion.Top, _dataRegion.Left, _dataRegion.Bottom + tick, _pen);
            canvas.DrawLine(_dataRegion.Right, _dataRegion.Top, _dataRegion.Right, _dataRegion.Bottom + tick, _pen);

            // Y axis
            _text.TextAlign = SKTextAlign.Left;
            float left = 2;
            canvas.DrawText($"{YUnits}", left, _dataRegion.Top + _dataRegion.Height / 2 + _text.FontMetrics.XHeight / 2, _text);
            canvas.DrawText($"{_yMin:0.00}", left, _dataRegion.Bottom + _text.FontMetrics.XHeight / 2, _text);
            canvas.DrawText($"{_yMax:0.00}", left, _dataRegion.Top + +_text.FontMetrics.XHeight / 2, _text);

            // X axis
            _text.TextAlign = SKTextAlign.Center;
            canvas.DrawText($"{XUnits}", _dataRegion.Left + _dataRegion.Width / 2, _dataRegion.Bottom + AXIS_SPACE, _text);
            canvas.DrawText($"{_xMin:0.00}", _dataRegion.Left, _dataRegion.Bottom + AXIS_SPACE, _text);
            canvas.DrawText($"{_xMax:0.00}", _dataRegion.Right, _dataRegion.Bottom + AXIS_SPACE, _text);
        }

        /// <summary>
        /// Draw the info display area.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SkControlInfo_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            SKCanvas canvas = e.Surface.Canvas;
            canvas.Clear();

            float xpos = 10;
            float ypos = 20;
            float yinc = 15;

            _text.Color = SKColors.Black;
            _text.TextAlign = SKTextAlign.Left;

            //canvas.DrawText($"M.X:{_currentMousePos.X:0.00}  M.Y:{_currentMousePos.Y:0.00}", xpos, ypos, _text);
            //ypos += yinc;

            //canvas.DrawText($"OS.X:{_dataOffset.X:0.00}  OS.Y:{_dataOffset.Y::0.00}", xpos, ypos, _text);
            //ypos += yinc;

            //canvas.DrawText($"Xmin:{_xMin:0.00}  Xmax:{_xMax:0.00}", xpos, ypos, _text);
            //ypos += yinc;

            //canvas.DrawText($"Ymin:{_yMin:0.00}  Ymax:{_yMax:0.00}", xpos, ypos, _text);
            //ypos += yinc;

            //ypos += yinc;

            canvas.DrawText($"Series:", xpos, ypos, _text);
            ypos += yinc;
            foreach (DataSeries ser in AllSeries)
            {
                _text.Color = ser.Color.ToSKColor();
                canvas.DrawText(ser.Name, xpos, ypos, _text);
                ypos += yinc;
            }
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Calculate geometry.
        /// </summary>
        void CalcGeometry()
        {
            // Do some geometry
            _dataRegion = new RectangleF(
                skControl.Left + BORDER_PAD + AXIS_SPACE,
                skControl.Top + BORDER_PAD,
                skControl.Width - BORDER_PAD - BORDER_PAD - AXIS_SPACE,
                skControl.Height - BORDER_PAD - BORDER_PAD - AXIS_SPACE);
        }

        /// <summary>
        /// Zoom function. Triggers redraw.
        /// </summary>
        /// <param name="level">If 0, reset. Constrains to 1 to 10.</param>
        void Zoom(double level)
        {
            if(level == 0)
            {
                _zoom = 1;
            }
            else
            {
                _zoom += level;
                _zoom = Constrain(_zoom, 1, 10);
            }

            Repaint();
        }

        /// <summary>
        /// Figure out min/max etc. Do some data fixups maybe.
        /// </summary>
        void InitData()
        {
            foreach (DataSeries ser in AllSeries)
            {
                // Spec the color if not supplied.
                if (ser.Color == Color.Empty)
                {
                    ser.Color = _colors[_colorIndex++ % _colors.Count];
                }

                // Find mins and maxes.
                foreach (DataPoint pt in ser.Points)
                {
                    _xMax = Math.Max(pt.X, _xMax);
                    _xMin = Math.Min(pt.X, _xMin);
                    _yMax = Math.Max(pt.Y, _yMax);
                    _yMin = Math.Min(pt.Y, _yMin);
                }
            }

            _xMax = Math.Ceiling(_xMax);
            _xMin = Math.Floor(_xMin);
            _yMax = Math.Ceiling(_yMax);
            _yMin = Math.Floor(_yMin);

            CalcGeometry();
        }

        /// <summary>
        /// Common updater.
        /// </summary>
        void Repaint()
        {
            Invalidate();
            Refresh();
        }

        /// <summary>Find the closest point to the given point.</summary>
        /// <param name="point">Mouse point</param>
        /// <returns>The closest DataPoint</returns>
        DataPoint GetClosestPoint(PointF point)
        {
            DataPoint closestPoint = null;
            const int SELECT_RANGE = 3;

            foreach (DataSeries series in AllSeries)
            {
                foreach (DataPoint p in series.Points)
                {
                    if (Math.Abs(point.X - p.ClientPoint.X) < SELECT_RANGE && Math.Abs(point.Y - p.ClientPoint.Y) < SELECT_RANGE)
                    {
                        closestPoint = p;
                    }
                }
            }

            return closestPoint;
        }

        /// <summary>
        /// Bounds limits a value.
        /// </summary>
        /// <param name="val"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        double Constrain(double val, double min, double max)
        {
            val = Math.Max(val, min);
            val = Math.Min(val, max);
            return val;
        }
        #endregion
    }
}
