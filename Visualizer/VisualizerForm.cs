using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Nebulator.Common;
using SkiaSharp;
using SkiaSharp.Views.Desktop;


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
        ///<summary></summary>
        public List<DataSeries> AllSeries { get; set; } = new List<DataSeries>();

        ///<summary></summary>
        public ChartType ChartType { get; set; } = ChartType.Scatter;

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
        ///<summary>Range for tooltips etc.</summary>
        const int MOUSE_SELECT_RANGE = 3;

        ///<summary>Data range.</summary>
        double _xMin = double.MaxValue;

        ///<summary>Data range.</summary>
        double _xMax = double.MinValue;

        ///<summary>Data range.</summary>
        double _yMin = double.MaxValue;

        ///<summary>Data range.</summary>
        double _yMax = double.MinValue;

        PointF _startMousePos = new PointF();
        PointF _endMousePos = new PointF();
        PointF _lastMousePos = new PointF();
        PointF _currentMousePos = new PointF();
        bool _mouseLeftDown = false;
        bool _mouseRightDown = false;
        //bool _firstPaint = true;

        ///<summary>Zoom ratio. 1 is all the way out aka home.</summary>
        double _zoom = 1;
        #endregion

        #region Geometry
        /// <summary>Whitespace around edges.</summary>
        const int BORDER_PAD = 20;

        /// <summary>Reserved for axes.</summary>
        const int AXIS_SPACE = 40;

        /// <summary>UI region to draw the dots.</summary>
        RectangleF _dataRegion = new RectangleF();

        ///// <summary>UI region to draw the axes.</summary>
        //RectangleF _axesRegion = new RectangleF();

        /// <summary>Displayed data is this far away from actual center.</summary>
        PointF _dataOffset = new PointF();
        #endregion

        #region Coloring
        /// <summary>Qualitative color set from http://colorbrewer2.org.</summary>
        List<Color> _colors1 = new List<Color>()
        {
            Color.FromArgb(27, 158, 119), Color.FromArgb(217, 95, 2),
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

            _colors.AddRange(_colors1);
            _colors.AddRange(_colors2);

            // Hook up handlers.
            KeyPress += VisualizerForm_KeyPress;

            MouseWheel += VisualizerForm_MouseWheel;
            
            skControl.Resize += SkControl_Resize;
            skControl.MouseDown += SkControl_MouseDown;
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
        private void SkControl_MouseDown(object sender, MouseEventArgs e)
        {
            _startMousePos = new Point(e.X, e.Y);
            _mouseLeftDown = e.Button == MouseButtons.Left;
            _mouseRightDown = !_mouseLeftDown;
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

            // Reset stuff.
            _mouseRightDown = false;
            _mouseLeftDown = false;
            _startMousePos = new Point();
            _endMousePos = new Point();
        }

        /// <summary>If the _mouseDown state is true then move the chart with the mouse.
        /// If the mouse is over a data point, show its coordinates.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SkControl_MouseMove(object sender, MouseEventArgs e)
        {
            _currentMousePos = new PointF(e.X, e.Y);

            if(_lastMousePos != _currentMousePos) // Apparently a known issue is that showing a tooltip causes a MouseMove event to get generated.
            {
                if (_mouseLeftDown) // move the chart
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
                else if(_mouseRightDown) // draw selection region TODON2
                {
                    _endMousePos = new Point(e.X, e.Y);
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
            hme.Handled = true; // This prevents the mouse wheel event from getting back to the parent.

            // If mouse is within control
            if (hme.X <= Width && hme.Y <= Height)
            {
                if (hme.Delta > 0)
                {
                    Zoom(0.5);
                }
                else
                {
                    Zoom(-0.5);
                }
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
                    Zoom(0.5);
                    break;

                case '-':
                case '_':
                    Zoom(-0.5);
                    break;

                case 'h':
                case 'H':
                    _dataOffset = new PointF();
                    Zoom(0);
                    //_firstPaint = true;
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

            // Create the transformer. or use math.net?
            // https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/graphics/skiasharp/transforms/matrix
            SKMatrix matrix = new SKMatrix();
            matrix.ScaleX = (float)(_zoom * (_dataRegion.Right - _dataRegion.Left) / (_xMax - _xMin));
            matrix.ScaleY = (float)(_zoom * (_dataRegion.Top - _dataRegion.Bottom) / (_yMax - _yMin));
            // Scale first? tx·sx ty·sy
            // matrix.TransX = (_dataOffset.X) * matrix.ScaleX;
            // matrix.TransX = (_dataOffset.X + AXIS_SPACE) * matrix.ScaleX;
            matrix.TransX = _dataOffset.X + _dataRegion.Left;
            matrix.TransY = _dataOffset.Y + _dataRegion.Bottom;
            matrix.Persp2 = 1;

            // Clip to drawing region.
            canvas.ClipRect(_dataRegion.ToSKRect());

            switch (ChartType)
            {
                case ChartType.Scatter:
                    DrawScatter(canvas, matrix);
                    break;

                case ChartType.Line:
                    DrawLines(canvas, matrix);
                    break;

                case ChartType.ScatterLine:
                    DrawLines(canvas, matrix);
                    DrawScatter(canvas, matrix);
                    break;
            }
        }

        /// <summary>
        /// Draw axes.
        /// </summary>
        /// <param name="canvas"></param>
        void DrawAxes(SKCanvas canvas) // TODON1
        {
            _pen.Color = SKColors.Black;

            // Draw area.
            SKPoint[] points = new SKPoint[4];
            points[0] = new SKPoint(_dataRegion.Left, _dataRegion.Top);
            points[1] = new SKPoint(_dataRegion.Right, _dataRegion.Top);
            points[2] = new SKPoint(_dataRegion.Right, _dataRegion.Bottom);
            points[3] = new SKPoint(_dataRegion.Left, _dataRegion.Bottom);
            SKPath path = new SKPath();
            path.AddPoly(points, true);
            _pen.StrokeWidth = 0.2f;
            canvas.DrawPath(path, _pen);

            // Axes.
            _pen.StrokeWidth = 2;
            // _xMax = (_xMax / XGrid + 1) * XGrid;
            // _xMin = (_xMin / XGrid) * XGrid;
            // _yMax = (_yMax / YGrid + 1) * YGrid;
            // _yMin = (_yMin / YGrid) * YGrid;

            // Y axis
            canvas.DrawLine(
                _dataRegion.Left - AXIS_SPACE,
                _dataRegion.Top,
                _dataRegion.Left - AXIS_SPACE,
                _dataRegion.Bottom + AXIS_SPACE,
                _pen);

            // X axis
            canvas.DrawLine(
                _dataRegion.Left - AXIS_SPACE,
                _dataRegion.Bottom + AXIS_SPACE,
                _dataRegion.Right,
                _dataRegion.Bottom + AXIS_SPACE,
                _pen);
        }

        /// <summary>
        /// Draw the scatter points.
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="matrix"></param>
        void DrawScatter(SKCanvas canvas, SKMatrix matrix)
        {
            foreach (DataSeries ser in AllSeries)
            {
                _pen.Color = ser.Color.ToSKColor();
                _pen.StrokeWidth = (float)DotSize;

                foreach (DataPoint pt in ser.Points)
                {
                    pt.ClientPoint = matrix.MapPoint(new SKPoint((float)pt.X, (float)pt.Y));
                    canvas.DrawPoint(pt.ClientPoint, _pen);
                }
            }
        }

        /// <summary>
        /// Draw line chart.
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="matrix"></param>
        void DrawLines(SKCanvas canvas, SKMatrix matrix)
        {
            foreach (DataSeries ser in AllSeries)
            {
                _pen.Color = ser.Color.ToSKColor();
                _pen.StrokeWidth = (float)LineSize;

                SKPoint[] points = new SKPoint[ser.Points.Count];
                for (int i = 0; i < ser.Points.Count; i++)
                {
                    ser.Points[i].ClientPoint = matrix.MapPoint(new SKPoint((float)ser.Points[i].X, (float)ser.Points[i].Y));
                    points[i] = ser.Points[i].ClientPoint;
                }

                SKPath path = new SKPath();
                path.AddPoly(points, false);
                canvas.DrawPath(path, _pen);
            }
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

            //void DrawText(SKCanvas canvas)
            float xpos = 10;
            float ypos = 20;
            float yinc = 15;

            _text.Color = SKColors.Black;

            //canvas.DrawText($"X:{_currentMousePos.X:0.00}  Y:{_currentMousePos.Y:0.00}", xpos, ypos, _text);
            //ypos += yinc;

            //canvas.DrawText($"OS.X:{_dataOffset.X:0.00}  OS.Y:{_dataOffset.Y::0.00}", xpos, ypos, _text);
            //ypos += yinc;

            canvas.DrawText($"Xmin:{_xMin:0.00}  Xmax:{_xMax:0.00}", xpos, ypos, _text);
            ypos += yinc;
            canvas.DrawText($"Ymin:{_yMin:0.00}  Ymax:{_yMax:0.00}", xpos, ypos, _text);
            ypos += yinc;

            // space
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

            //_axesRegion = new RectangleF(
            //    skControl.Left + BORDER_PAD,
            //    skControl.Top + BORDER_PAD,
            //    skControl.Width - BORDER_PAD - BORDER_PAD,
            //    skControl.Height - BORDER_PAD - BORDER_PAD);
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
                _zoom = Utils.Constrain(_zoom, 1, 10);
            }

            Repaint();
        }

        /// <summary>
        /// Figure out min/max etc. Do some data fixups maybe.
        /// </summary>
        void InitData()
        {
            //_firstPaint = true;

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

            foreach (DataSeries series in AllSeries)
            {
                foreach (DataPoint p in series.Points)
                {
                    if (Math.Abs(point.X - p.ClientPoint.X) < MOUSE_SELECT_RANGE && Math.Abs(point.Y - p.ClientPoint.Y) < MOUSE_SELECT_RANGE)
                    {
                        closestPoint = p;
                    }
                }
            }

            return closestPoint;
        }
        #endregion



        // probably get rid of these
        private void BtnPlus_Click(object sender, EventArgs e)
        {
            Zoom(0.5);
        }

        private void BtnMinus_Click(object sender, EventArgs e)
        {
            Zoom(-0.5);
        }
    }
}
