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

// The Equality Operator (==) is the comparison operator and the Equals() method compares the contents of a string.
// The == Operator compares the reference identity while the Equals() method compares only contents.

namespace Nebulator.Visualizer
{
    /// <summary>
    /// A simple display of numerical series.
    /// </summary>
    public partial class VisualizerForm : Form
    {
        public enum ChartTypes { Line, Scatter, ScatterLine };

        #region Properties for client customization
        ///<summary></summary>
        public List<DataSeries> AllSeries { get; set; } = new List<DataSeries>();

        ///<summary></summary>
        public ChartTypes ChartType { get; set; } = ChartTypes.Scatter;

        ///<summary></summary>
        public double DotSize { get; set; } = 3;

        ///<summary></summary>
        public double LineSize { get; set; } = 1;

        ///<summary>Space between grid lines. Default is 0 which means no lines.</summary>
        public double XGrid { get; set; } = 0;

        ///<summary>Space between grid lines. Default is 0 which means no lines.</summary>
        public double YGrid { get; set; } = 0;

        // Others:
        // AxesColor = Color.RosyBrown;
        // AxesWidth = 1;
        // XNumTicks = 10;
        // YNumTicks = 10;
        // GridLineColor = Color.LightGray;
        // ChartBackColor = Color.White;

        #endregion


        #region Fields
        const int MOUSE_SELECT_RANGE = 5;

        // The calculated ranges.
        double _xMin = double.MaxValue;
        double _xMax = double.MinValue;
        double _yMin = double.MaxValue;
        double _yMax = double.MinValue;

       // bool _dragging = false;
        bool _mouseLeftDown = false;
        bool _mouseRightDown = false;
        bool _firstPaint = true;
        //bool _ctrlDown = false;
        //bool _shiftDown = false;

        PointF _startMousePos = new PointF();
        PointF _endMousePos = new PointF();
        PointF _lastMousePos = new PointF();

        // UI location to put the dots.
        RectangleF _drawArea = new RectangleF();

        // Bottom left corner of current _drawArea.
        PointF _origin = new PointF();

        double _zoom = 1; // 1 is all the way out aka home

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

        #region Data series support TODON2 probably common
        public class DataPoint
        {
            public DataSeries Owner { get; set; } = null;
            // Data points in normal x/y client coordinates.
            public double X { get; set; } = 0;
            public double Y { get; set; } = 0;

            // Where currently in the UI.
            public SKPoint ClientPoint { get; set; }
        
            public override string ToString()
            {
                return $"X:{X}Y:{Y}{Environment.NewLine}{Owner.Name}";
            }
        }

        ///<summary></summary>
        public class DataSeries
        {
            ///<summary></summary>
            public string Name { get; set; } = "No Name";

            ///<summary></summary>
            public Color Color { get; set; } = Color.Empty;

            ///<summary>Data points in normal x/y coordinates.</summary>
            public List<DataPoint> Points { get; set; } = new List<DataPoint>();

            ///<summary></summary>
            public void AddPoint(double x, double y)
            {
                Points.Add(new DataPoint() { X = x, Y = y, Owner = this });
            }
        }
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
            //skControl.Dock = DockStyle.Fill;

            _colors.AddRange(_colors1);
            _colors.AddRange(_colors2);

            // Hook up handlers.
            skControl.Resize += SkControl_Resize;
            //skControl.LostFocus += SkControl_LostFocus;
            //skControl.KeyDown += SkControl_KeyDown;
            //skControl.KeyUp += SkControl_KeyUp;
            skControl.KeyPress += SkControl_KeyPress;
            skControl.MouseWheel += SkControl_MouseWheel;
            skControl.MouseDown += SkControl_MouseDown;
            skControl.MouseUp += SkControl_MouseUp;
            skControl.MouseMove += SkControl_MouseMove;
            skControl.MouseClick += SkControl_MouseClick;
            skControl.PaintSurface += SkControl_PaintSurface;

            toolTip.AutomaticDelay = 0;
            toolTip.AutoPopDelay = 0;
            toolTip.InitialDelay = 300;
            toolTip.ReshowDelay = 0;
            toolTip.UseAnimation = false;
            toolTip.UseFading = false;

            // Assumes user has populated series.
            Init();
            

            
        }
        #endregion

        #region Mouse Event Handlers
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

        private void SkControl_MouseDown(object sender, MouseEventArgs e)
        {
            _startMousePos = new Point(e.X, e.Y);
            _mouseLeftDown = e.Button == MouseButtons.Left;
            _mouseRightDown = !_mouseLeftDown;
        }

        private void SkControl_MouseUp(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Right)
            {
                // TODON1 Set chart to selection area. Must update zoom values too.


                Repaint();
            }
            else // Left
            {
                // Finished dragging.
                //Repaint();
            }

            // Reset stuff.
            _mouseRightDown = false;
            _mouseLeftDown = false;
            _startMousePos = new Point();
            _endMousePos = new Point();

        }

        /// <summary>If the _mouseDown state is true then move the chart with the mouse.
        /// If the mouse is over a data point, show it's coordinates.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SkControl_MouseMove(object sender, MouseEventArgs e)
        {
            PointF newPos = new PointF(e.X, e.Y);

            if(_lastMousePos != newPos) // Apparently a known issue is that showing a tooltip causes a MouseMove event to get generated.
            {
                // If the _mouseDown state is true then move the chart with the mouse. TODON1
                if (_mouseLeftDown)
                {
                    float xChange = newPos.X - _lastMousePos.X;
                    float yChange = newPos.Y - _lastMousePos.Y;

                    // If there is a change in x or y...
                    if (xChange > 0 || yChange > 0)
                    {
                        // Adjust the axes
                        _origin.Y += yChange;
                        _origin.X += xChange;

                        Repaint();
                    }
                }
                else if(_mouseRightDown)
                {
                    // Do some special stuff to show a rectangle selection box. TODON1
                    _endMousePos = new Point(e.X, e.Y);

                    Repaint();
                }
                else
                {
                    // If the mouse is over a point or cursor, show its tooltip.

                    DataPoint closestPoint = GetClosestPoint(newPos);

                    if (closestPoint != null)
                    {
                        // Display the tooltip
                        toolTip.Show(closestPoint.ToString(), this, (int)(newPos.X + 15), (int)(newPos.Y));
                    }
                    else
                    {
                        // Hide the tooltip
                        toolTip.Hide(this);
                    }
                }

                _lastMousePos = newPos;
            }
        }

        /// <summary>Zooms in or out depending on the direction the mouse wheel is moved.</summary>
        /// <param name="sender">Object that sent triggered the event.</param>
        /// <param name="e">The particular MouseEventArgs (DoubleClick, Click, etc.).</param>
        private void SkControl_MouseWheel(object sender, MouseEventArgs e)
        {
            HandledMouseEventArgs hme = (HandledMouseEventArgs)e;
            hme.Handled = true; // This prevents the mouse wheel event from getting back to the parent.

            // If mouse is within control
            if (hme.X <= Width && hme.Y <= Height)
            {
                if (hme.Delta > 0) // clicks?
                {
                    ZoomIn();
                }
                else
                {
                    ZoomOut();
                }
            }
        }

        #endregion

        #region Key Event Handlers
        private void SkControl_KeyPress(object sender, KeyPressEventArgs e)
        {
            switch (char.ToUpper(e.KeyChar))
            {
                case '+':
                    ZoomIn();
                    break;

                case '-':
                    ZoomOut();
                    break;

                case 'h':
                case 'H':
                    _firstPaint = true;
                    Repaint();
                    break;
            }
        }
        #endregion

        #region Window Event Handlers
        private void SkControl_Resize(object sender, EventArgs e)
        {
            CalcSize();
            Repaint();
        }
        #endregion

        #region Render functions
        void SkControl_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            SKCanvas canvas = e.Surface.Canvas;
            canvas.Clear();

            DrawText(canvas);

            switch (ChartType)
            {
                case ChartTypes.Scatter:
                    DrawScatter(canvas);
                    break;

                case ChartTypes.Line:
                    DrawLines(canvas);
                    break;

                case ChartTypes.ScatterLine:
                    DrawLines(canvas);
                    DrawScatter(canvas);
                    break;
            }
        }

        void DrawText(SKCanvas canvas)
        {
            float xpos = 10;
            float ypos = 20;
            float yinc = 15;

            _text.Color = SKColors.Black;
            canvas.DrawText($"Xmin:{_xMin}  Xmax:{_xMax}", xpos, ypos, _text);
            ypos += yinc;
            canvas.DrawText($"Ymin:{_yMin}  Ymax:{_yMax}", xpos, ypos, _text);
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

        void DrawScatter(SKCanvas canvas)
        {
            foreach (DataSeries ser in AllSeries)
            {
                _pen.Color = ser.Color.ToSKColor();
                _pen.StrokeWidth = (float)DotSize;

                foreach (DataPoint pt in ser.Points)
                {
                    MapData(pt); // TODON2 shouldn't need to do this every time... _firstPaint??
                    canvas.DrawPoint(pt.ClientPoint, _pen);
                }
            }
        }

        void DrawLines(SKCanvas canvas)
        {
            foreach (DataSeries ser in AllSeries)
            {
                _pen.Color = ser.Color.ToSKColor();
                _pen.StrokeWidth = (float)LineSize;

                SKPoint[] points = new SKPoint[ser.Points.Count];
                for (int i = 0; i < ser.Points.Count; i++)
                {
                    MapData(ser.Points[i]); //TODON2 shouldn't need to do this every time...
                    points[i] = ser.Points[i].ClientPoint;
                }

                SKPath path = new SKPath();
                path.AddPoly(points, false);

                canvas.DrawPath(path, _pen);
            }
        }

        #endregion




        #region Private functions

        void CalcSize()
        {
            // Do some geometry
            _drawArea = new RectangleF(skControl.Left + 150, skControl.Top + 20, skControl.Width - 170, skControl.Height - 40);

            // Force repaint of chart.
            _firstPaint = true; // Need to recalc the grid too.

        }

        void ZoomIn()
        {
            _zoom = Math.Min(_zoom + 1, 10);
            Repaint();
        }

        void ZoomOut()
        {
            _zoom = Math.Max(_zoom - 1, 1);
            Repaint();
        }

        /// <summary>
        /// Convert a native point to screen coords.
        /// </summary>
        /// <param name="pt"></param>
        void MapData(DataPoint pt)
        {
            double x = Map(pt.X, _xMin, _xMax, _drawArea.Left, _drawArea.Right);
            double y = Map(pt.Y, _yMin, _yMax, _drawArea.Bottom, _drawArea.Top); // inverted!
            pt.ClientPoint = new SKPoint((float)x, (float)y);
        }

        double Map(double val, double start1, double stop1, double start2, double stop2)
        {
            return start2 + (stop2 - start2) * (val - start1) / (stop1 - start1);
        }

        /// <summary>Do some data fixups maybe.</summary>
        void Init()
        {
            _firstPaint = true;

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

            if (XGrid == 0)
            {
                _xMax = Math.Ceiling(_xMax);
                _xMin = Math.Floor(_xMin);
            }
            else
            {
                _xMax = (_xMax / XGrid + 1) * XGrid;
                _xMin = (_xMin / XGrid) * XGrid;
            }

            if (YGrid == 0)
            {
                _yMax = Math.Ceiling(_yMax);
                _yMin = Math.Floor(_yMin);
            }
            else
            {
                _yMax = (_yMax / YGrid + 1) * YGrid;
                _yMin = (_yMin / YGrid) * YGrid;
            }

            CalcSize();
        }

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

            //foreach (DataSeries series in AllSeries)
            //{
            //    foreach (DataPoint p in series.Points)
            //    {
            //        if (Math.Abs(point.X - p.ClientPoint.X) < MOUSE_SELECT_RANGE && Math.Abs(point.Y - p.ClientPoint.Y) < MOUSE_SELECT_RANGE)
            //        {
            //            closestSeries = series;
            //            closestPoint = p;
            //        }
            //    }
            //}

            return closestPoint;
        }
        #endregion
    }


}
