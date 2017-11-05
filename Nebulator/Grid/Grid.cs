using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;


// A general purpose grid for midi notes etc. WIP.


// prop grid
// --------------
// Defs:
//     const(PART1, 0);
//     const(DRUM_DEF_VOL, 100);
//     var(COL1, 200); // change color
//     var(MODN, 0); // modulate notes
//     var(PITCH, 8192); // center is 8192
//     midiin(1, 2, MODN);
//     midiout(1, Pitch, PITCH);
//     lever(-10, 10, MODN);

// edit section
// ---------------
// tracks:
//     track-name
//     channel
//     wobbles
//     loops - as blocks on the top grid

// Seqs:
//     seq-name
//     lenticks
//     notes - as blocks on the bottom grid:
//         name
//         vol
//         when
//         dur

// Grids: drag/drop, drag end(s), snap to grid, enter offset from grid,



namespace Nebulator.Grid
{
    public partial class Grid : UserControl
    {
        List<float> xVals = new List<float>();
        List<float> yVals = new List<float>();
        float xRange = 0;
        float yRange = 0;
        Color _dotColor = Color.Purple;
        List<DataPoint> _data = new List<DataPoint>();

        public Grid()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            InitializeComponent();
        }

        private void Grid_Load(object sender, EventArgs e)
        {
            //I am trying to add vertical and horizontal scrollbars to my UserControl with the HorizontalScroll and 
            // VerticalScroll properties, but I am having extreme issues.My problem arises when I drag or manipulate the 
            //scroll box on the bar. When I let it go, it simply jumps back to the start position!
            //I know of the AutoScroll property, but I do not want to use it since I want to be able to control every 
            //aspect of my scrollbars, and I don't want it to be done automatically. Also, according to the documentation, 
            //AutoScroll is for "[enabling] the user to scroll to any controls placed outside of its visible boundaries" 
            //which isn't what I want. I just want scrollbars.
            //...aaand I suppose I could add VScrollBar and HScrollBar to the control, but why should I do this when the 
            //scroll functionality already exists? Seems like a waste to me.

            //Set the AutoScrollMinSize property.
            //If you implemented the OnPaint() override then you'll need to use the AutoScrollPosition property to 
            //set the arguments for e.Graphics.TranslateTransform().

            AutoScroll = true;
            AutoScrollMinSize = new Size(1, 1);

        }

        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                e.Graphics.Clear(ChartBackColor);

                // Test for no data.
                bool hasData = _data.Count > 0;

                if (!hasData)
                {
                    e.Graphics.DrawString("NO DATA",
                        new Font(FontFamily.GenericSansSerif, 0.03F * Width),
                        Brushes.Red,
                        new PointF(0.3F * Width, Height / 2));
                }
                else
                {
                    // If first paint reset zoom scales.
                    if (_firstPaint)
                    {
                        Init();
                    }

                    // Update scales.
                    _xMinScale = (_dataRect.Left - _origin.X) / _xZoomedScale;
                    _xMaxScale = (_dataRect.Right - _origin.X) / _xZoomedScale;
                    _yMinScale = (_origin.Y - _dataRect.Bottom) / _yZoomedScale;
                    _yMaxScale = (_origin.Y - _dataRect.Top) / _yZoomedScale;

                    // Only recalculate tick values when zoomed scale changes or first paint
                    if (_zoomed || _firstPaint)
                    {
                        _xTickVal = GetTickValue(0.001f, _xMaxScale - _xMinScale, XNumTicks, true);
                        _xTickVal = _xTickVal < 0.001F ? 0.001F : _xTickVal;
                        //_xTickVal = (int)(GetTickValue(1, _xMaxScale - _xMinScale, XNumTicks, true) + 0.5);
                        //_xTickVal = _xTickVal == 0 ? 1 : _xTickVal;

                        _yTickVal = GetTickValue(0.001f, _yMaxScale - _yMinScale, YNumTicks, false);
                        _yTickVal = _yTickVal < 0.001F ? 0.001F : _yTickVal;
                        //_yTickVal = (int)(GetTickValue(1, _yMaxScale - _yMinScale, YNumTicks, false) + 0.5);
                        //_yTickVal = _yTickVal == 0 ? 1 : _yTickVal;

                        // Set zoom dirty flag to false
                        _zoomed = false;
                    }

                    // Draw the grid and axes.
                    List<StringFloatPair> xticks;
                    List<StringFloatPair> yticks;
                    DrawGrid(e.Graphics, out xticks, out yticks);

                    #region Series Charting
                    bool lineSeriesDrawn = false;

                    // Draw the ChartType specified for each series in the collection.
                    DrawData(e.Graphics);
                    #endregion

                    //// Draw Cursors
                    //foreach (ChartCursor cursor in _cursors)
                    //{
                    //    DrawCursor(e.Graphics, cursor);
                    //}

                    if (lineSeriesDrawn)
                    {
                        // Clear region outside axes.
                        ClearNonViewableRegion(e.Graphics);
                    }

                    // Draw the ticks
                    DrawTicks(e.Graphics, xticks, yticks);

                    #region Draw labels if supplied.
                    int ws = 3; // clearance for labels
                    if (!string.IsNullOrEmpty(TitleLabel))
                    {
                        if (TitleAlignment == HorizontalAlignment.Left)
                        {
                            Point newLoc = new Point(10, ws);
                            DrawLabel(e.Graphics, newLoc.X, newLoc.Y, TitleLabel, _titleLabelFont, 0);
                        }
                        else // default to center - add right later if needeed
                        {
                            float twidth = e.Graphics.MeasureString(TitleLabel, _titleLabelFont).Width;
                            Point newLoc = new Point(Width / 2 - (int)twidth / 2, ws);
                            DrawLabel(e.Graphics, newLoc.X, newLoc.Y, TitleLabel, _titleLabelFont, 0);
                        }
                    }

                    if (!string.IsNullOrEmpty(XAxisLabel))
                    {
                        float twidth = e.Graphics.MeasureString(XAxisLabel, _axisLabelFont).Width;
                        Point newLoc = new Point(Width / 2 - (int)twidth / 2, Height - _axisLabelFont.Height - ws);
                        DrawLabel(e.Graphics, newLoc.X, newLoc.Y, XAxisLabel, _axisLabelFont, 0);
                    }

                    if (!string.IsNullOrEmpty(YAxisLabel))
                    {
                        float twidth = e.Graphics.MeasureString(YAxisLabel, _axisLabelFont).Width;
                        float theight = _axisLabelFont.Height;
                        DrawLabel(e.Graphics, ws, Height / 2 + twidth / 2, YAxisLabel, _axisLabelFont, 270);
                    }
                    #endregion

                    #region Draw Selection Rectangle
                    // Draw Selection Rectangle if ctrl is down and not dragging cursor.
                    if (_ctrlDown && _dragging && _dragCursor == null)
                    {
                        float width = 0.0f, height = 0.0f;
                        Point startPoint = new Point();

                        if (_endMousePos.X > _startMousePos.X)
                        {
                            width = _endMousePos.X - _startMousePos.X;
                            startPoint.X = _startMousePos.X;
                        }
                        else
                        {
                            width = _startMousePos.X - _endMousePos.X;
                            startPoint.X = _endMousePos.X;
                        }

                        if (_endMousePos.Y > _startMousePos.Y)
                        {
                            height = _endMousePos.Y - _startMousePos.Y;
                            startPoint.Y = _startMousePos.Y;
                        }
                        else
                        {
                            height = _startMousePos.Y - _endMousePos.Y;
                            startPoint.Y = _endMousePos.Y;
                        }

                        e.Graphics.DrawRectangle(new Pen(Color.Gray), startPoint.X, startPoint.Y, width, height);
                    }
                    #endregion
                }

                // Draw the Border
                e.Graphics.DrawRectangle(new Pen(Color.RoyalBlue, 1), new Rectangle(0, 0, Width - 1, Height - 1));

                _firstPaint = false;
            }
            catch
            {
                e.Graphics.DrawLine(new Pen(Color.Red), new Point(0, 0), new Point(Right, Bottom));
                e.Graphics.DrawLine(new Pen(Color.Red), new Point(Right, 0), new Point(0, Bottom));
            }
        }


        ////////////////// From XXX ////////////////////////////



        #region Constants
        /// <summary>Length of the tick marks in pixels.</summary>
        private const int TICK_SIZE = 5;

        /// <summary>Maximum zoom in limit.</summary>
        private const int MAX_ZOOM_LIMIT_OTHER = 100;

        /// <summary>Minimum zoom out limit.</summary>
        private const float MIN_ZOOM_LIMIT = 0.1f;

        /// <summary>How close do you have to be to select a feature.</summary>
        private const int MOUSE_SELECT_RANGE = 5;
        #endregion

        #region Private Fields
        #region Miscellaneous
        /// <summary>Are hidden points visible.</summary>
        private bool _showHidden = true;

        /// <summary>The area to draw the data points in pixels.</summary>
        private RectangleF _dataRect = new RectangleF();

        /// <summary>Logical data origin in pixels.</summary>
        private PointF _origin = new PointF();

        /// <summary>Init flag.</summary>
        private bool _firstPaint = true;
        #endregion

        #region Cursors
        /// <summary>Current cursor set.</summary>
        private List<GridCursor> _cursors = new List<GridCursor>();

        /// <summary>If non-null identifies the cursor being dragged now.</summary>
        private GridCursor _dragCursor = null;
        #endregion

        #region Grid
        /// <summary>Default x tick value.</summary>
        private float _xTickVal = 1;

        /// <summary>Default y tick value.</summary>
        private float _yTickVal = 1;

        /// <summary>Font to be used when drawing the labels.</summary>
        private Font _axisLabelFont = new Font("Arial", 9, System.Drawing.FontStyle.Bold);

        /// <summary>Font to be used when drawing the labels.</summary>
        private Font _titleLabelFont = new Font("Arial", 11, System.Drawing.FontStyle.Bold);

        /// <summary>Font to be used when drawing the text tick labels.</summary>
        private Font _tickFont = new Font("Arial", 7);
        #endregion

        #region Scales
        /// <summary>X min in data units.</summary>
        private float _xMinScale = float.MinValue;

        /// <summary>X max in data units.</summary>
        private float _xMaxScale = float.MaxValue;

        /// <summary>Y min in data units.</summary>
        private float _yMinScale = float.MinValue;

        /// <summary>Y max in data units.</summary>
        private float _yMaxScale = float.MaxValue;

        /// <summary>X Scale of data range vs viewable chart range</summary>
        private float _xActualScale = float.MaxValue;

        /// <summary>Y Scale of data range vs viewable chart range</summary>
        private float _yActualScale = float.MaxValue;
        #endregion

        #region Keys
        /// <summary>If y is pressed.</summary>
        private bool _yDown = false;

        /// <summary>If x is pressed.</summary>
        private bool _xDown = false;

        /// <summary>If control is pressed.</summary>
        private bool _ctrlDown = false;

        /// <summary>If shift is pressed.</summary>
        private bool _shiftDown = false;
        #endregion

        #region Mouse States
        /// <summary>Saves state as to whether left button is down.</summary>
        private bool _mouseDown = false;

        /// <summary>Current mouse position.</summary>
        private Point _startMousePos = new Point();

        /// <summary>End mouse position.</summary>
        private Point _endMousePos = new Point();

        /// <summary>Boolean to determine if the user is dragging the mouse.</summary>
        private bool _dragging = false;
        #endregion

        #region Zooming and shift
        /// <summary>Whether the chart zoom has changed.</summary>
        private bool _zoomed = false;

        /// <summary>Speed at which to shift the control (left/right/up/down).</summary>
        private int _shiftSpeed = 5;

        /// <summary>Speed at which to zoom in/out.</summary>
        private float _zoomSpeed = 1.1f; // 1.25f is too fast

        /// <summary>Current X scale factor based on _xZoomFactor.</summary>
        private float _xZoomedScale = float.MaxValue;

        /// <summary>Current Y scale factor based on _yZoomFactor.</summary>
        private float _yZoomedScale = float.MaxValue;

        /// <summary>Current user selected zoom factor.</summary>
        private float _xZoomFactor = 1.0f;

        /// <summary>Current user selected zoom factor.</summary>
        private float _yZoomFactor = 1.0f;
        #endregion
        #endregion

        #region Events
        /// <summary>Scale change event class.</summary>
        public class ScaleChangeEventArgs : EventArgs
        {
            public float xMinScale = float.MinValue;
            public float xMaxScale = float.MaxValue;
            public float yMinScale = float.MinValue;
            public float yMaxScale = float.MaxValue;
        }

        /// <summary>Publish event for scale change.</summary>
        public event EventHandler<ScaleChangeEventArgs> ScaleChange = delegate { };

        /// <summary>Fire the event for scale change.</summary>
        void FireScaleChange()
        {
            ScaleChange(this, new ScaleChangeEventArgs() { xMinScale = _xMinScale, xMaxScale = _xMaxScale, yMinScale = _yMinScale, yMaxScale = _yMaxScale });
        }

        /// <summary>Cursor move event class.</summary>
        public class ChartCursorMoveEventArgs : EventArgs
        {
            public int CursorId { get; set; }
            public float Position { get; set; }
        }

        /// <summary>Publish event for scale change.</summary>
        public event EventHandler<ChartCursorMoveEventArgs> ChartCursorMove = delegate { };

        // Tool Tip Provider Events  TODO these:
        /// <summary>Definition of delegate for getting tooltip text to display.</summary>
        /// <param name="id">The id column value.</param>
        /// <returns>The string if available.</returns>
        public delegate string ToolTipProviderX(int id);

        /// <summary>Definition of delegate for getting tooltip text to display.</summary>
        /// <param name="dp">The data point.</param>
        /// <returns>The string if available.</returns>
        public delegate string DataPointToolTipProviderX(DataPoint dp);

        /// <summary>Definition of delegate for getting tooltip text to display.</summary>
        /// <param name="id">The cursor id.</param>
        /// <returns>The string if available.</returns>
        public delegate string CursorToolTipProviderX(int id);

        ///<summary>Gets or sets the data point tool tip provider</summary>
        public DataPointToolTipProviderX DPToolTipProvider { get; set; }

        ///<summary>Gets or sets the chart tool tip provider</summary>
        public ToolTipProviderX ChartToolTipProvider { get; set; }

        ///<summary>Gets or sets the chart tool tip provider</summary>
        public CursorToolTipProviderX CursorToolTipProvider { get; set; }
        #endregion

        /// <summary>Saves the previous mouse position.</summary>
        public Point LastMousePos { get; private set; } = new Point(int.MaxValue, int.MaxValue);

        /// <summary>List of selected DataPoints.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<DataPoint> SelectedPoints { get; set; } = new List<DataPoint>();

        ///<summary>Gets or sets if navigation is allowed.</summary>
        public bool AllowNavigation { get; set; } = true;

        ///<summary>Gets or sets whether to suppress particular events handled higher up.</summary>
        public bool SuppressEvents { get; set; } = true;

        #region Labels if not using ChartLegend and TitleBar
        /// <summary>The title label.</summary>
        [Category("Labels")]
        [Description("Title Label")]
        public string TitleLabel { get; set; } = "TITLE";

        /// <summary>The title label.</summary>
        [Category("Labels")]
        [Description("Title Label")]
        public HorizontalAlignment TitleAlignment { get; set; } = HorizontalAlignment.Center;

        /// <summary>The X axis label.</summary>
        [Category("Labels")]
        [Description("X Axis Label")]
        public string XAxisLabel { get; set; } = "XXXX";

        /// <summary>The Y axis label.</summary>
        [Category("Labels")]
        [Description("Y Axis Label")]
        public string YAxisLabel { get; set; } = "YYYY";
        #endregion

        #region Axes
        /// <summary>The color to be used when drawing the Axes.</summary>
        [Category("Axes")]
        [Description("Color of both Axes")]
        public Color AxesColor { get; set; } = Color.RosyBrown;

        /// <summary>The width to use for the axes in pixels.</summary>
        [Category("Axes")]
        [Description("Width of Axes")]
        public float AxesWidth { get; set; } = 1;

        /// <summary>The number of ticks to use on the axis. If 0, it is autogenerated.</summary>
        [Category("Axes")]
        [Description("Number of X ticks")]
        public int XNumTicks { get; set; } = 10;

        /// <summary>Show or hide the labels.</summary>
        [Category("Axes")]
        [Description("X labels visible")]
        public bool XLabelsVisible { get; set; } = true;

        /// <summary>The number of ticks to use on the axis. If 0, it is autogenerated.</summary>
        [Category("Axes")]
        [Description("Number of Y ticks")]
        public int YNumTicks { get; set; } = 10;

        /// <summary>Show or hide the labels.</summary>
        [Category("Axes")]
        [Description("Y labels visible")]
        public bool YLabelsVisible { get; set; } = true;
        #endregion

        #region Grid Colors
        /// <summary>Color to be used when drawing the gridlines.</summary>
        [Category("Grid")]
        [Description("Gridline color")]
        public Color GridLineColor { get; set; } = Color.LightGray;

        /// <summary>Color to be used when drawing the background of the chart.</summary>
        [Category("Grid")]
        [Description("Background Color of Chart")]
        public Color ChartBackColor { get; set; } = Color.White;
        #endregion

        /// <summary>Constructs a new chart and initializes the series collection.</summary>
        /// <param name="data">A collection 1-N series</param>
        public void Init(List<PointF> data)
        {
            int pi = 1;

            _data.Clear();
            foreach(PointF pt in data)
            {
                _data.Add(new DataPoint(pt, "PX", "PY", pi++));
            }

            xVals = data.Select(i => i.X).ToList();
            yVals = data.Select(i => i.Y).ToList();
            xRange = xVals.Max() - xVals.Min();
            yRange = yVals.Max() - yVals.Min();

            _firstPaint = true;

            toolTip.AutomaticDelay = 0;
            toolTip.AutoPopDelay = 0;
            toolTip.InitialDelay = 300;
            toolTip.ReshowDelay = 0;
            toolTip.UseAnimation = false;
            toolTip.UseFading = false;


            // Set the default scales.
            _xMinScale = xVals.Min();
            _xMaxScale = xVals.Max();
            _yMinScale = yVals.Min();
            _yMaxScale = yVals.Max();

            // UI handlers.
            KeyDown += new KeyEventHandler(Grid_KeyDown);
            KeyUp += new KeyEventHandler(Grid_KeyUp);
            MouseDown += new MouseEventHandler(Grid_MouseDown);
            MouseMove += new MouseEventHandler(Grid_MouseMove);
            MouseUp += new MouseEventHandler(Grid_MouseUp);
            MouseWheel += new MouseEventHandler(Grid_MouseWheel);
            MouseClick += new MouseEventHandler(Grid_MouseClick);
            LostFocus += new EventHandler(Grid_LostFocus);
            Resize += new EventHandler(Grid_Resize);

            Invalidate();
        }

        /// <summary>Perform chart initialization.</summary>
        private void Init()
        {
            // Set Axis positions. Arbitrary values based on the string sizes.
            float bottom = 0.0f;
            float left = Width * 0.1f; // default for float
            bottom = Height * 0.9f;

            _dataRect = new RectangleF(left, 25.0f, Width - left - 5.0f, bottom - 30.0f);

            // Calculate scale for data range to fit on initial chart.
            _xActualScale = float.MaxValue;
            _yActualScale = float.MaxValue;

            // single value or pixels per data unit
            _xActualScale = _dataRect.Width / xRange;
            _yActualScale = _dataRect.Height / yRange;

            // Calculate the origin X --> buffered by ([1-_xZoomFactor]/2).
            _origin.X = xRange != 0 ?
                _dataRect.Left - xVals.Min() * _xActualScale :
                _dataRect.Left - (xVals.Min() * _xActualScale) + (_dataRect.Width / 2);

            // Calculate the orgin Y --> buffered by ([1-_yZoomFactor]/2).
            _origin.Y = yRange != 0 ?
                _dataRect.Bottom + (yVals.Min() * _yActualScale) :
                _dataRect.Bottom + (yVals.Min() * _yActualScale) - (_dataRect.Height / 2);

            // Reset zoom scales.
            _xZoomFactor = 0.95f;
            _yZoomFactor = 0.95f;

            Recenter(_xZoomFactor, _yZoomFactor);

            _xZoomedScale = _xActualScale * _xZoomFactor;
            _yZoomedScale = _yActualScale * _yZoomFactor;

            // Update scales.
            _xMinScale = (_dataRect.Left - _origin.X) / _xZoomedScale;
            _xMaxScale = (_dataRect.Right - _origin.X) / _xZoomedScale;
            _yMinScale = (_origin.Y - _dataRect.Bottom) / _yZoomedScale;
            _yMaxScale = (_origin.Y - _dataRect.Top) / _yZoomedScale;
        }

        ///// <summary>Redraws the screen.</summary>
        //public void ReInit()
        //{
        //    _firstPaint = true;
        //    Refresh();
        //}

        ///// <summary>Center the chart using visible series only.</summary>
        //public void CenterVisible()
        //{
        //    SeriesCollection.UpdateSeriesBounds(true, false);

        //    // Force a re-Paint to update.
        //    ReInit();
        //}

        /// <summary>Find the closest point to the given point.</summary>
        /// <param name="point">Mouse point</param>
        /// <returns>The closest DataPoint</returns>
        public DataPoint GetClosestPoint(Point point)
        {
            DataPoint closestPoint = null;
            foreach (DataPoint p in _data)
            {
                if (Math.Abs(point.X - p.ClientPoint.X) < MOUSE_SELECT_RANGE && Math.Abs(point.Y - p.ClientPoint.Y) < MOUSE_SELECT_RANGE)
                {
                    closestPoint = p;
                }
            }

            return closestPoint;
        }


        void Grid_Resize(object sender, EventArgs e)
        {
            // Force repaint of chart.
            _firstPaint = true; // Need to recalc the grid too.
            Invalidate();
            Refresh();
        }

        /// <summary>Resets key states when control loses focus.</summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event Arguments</param>
        void Grid_LostFocus(object sender, EventArgs e)
        {
            _ctrlDown = false;
            _shiftDown = false;
        }

        /// <summary>Zooms in or out depending on the direction the mouse wheel is moved.</summary>
        /// <param name="sender">Object that sent triggered the event.</param>
        /// <param name="e">The particular MouseEventArgs (DoubleClick, Click, etc.).</param>
        private void Grid_MouseWheel(object sender, MouseEventArgs e)
        {
            HandledMouseEventArgs hme = (HandledMouseEventArgs)e;
            hme.Handled = true; // This prevents the mouse wheel event from getting back to the parent.

            // If mouse is within control
            if (hme.X <= Width && hme.Y <= Height)
            {
                if (hme.Delta > 0)
                {
                    ZoomIn();
                }
                else
                {
                    ZoomOut();
                }

                _zoomed = true;

                FireScaleChange();
            }
        }

        /// <summary>Sets the _mouseDown state to false.</summary>
        /// <param name="sender">Object that sent triggered the event.</param>
        /// <param name="e">The particular MouseEventArgs (DoubleClick, Click, etc.).</param>
        private void Grid_MouseUp(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    if (_ctrlDown) // TODO Set chart to selection area. Must update zoom values too.
                    {
                        // Maybe use Recenter()

                        ////// This is the original OARS behavior: to select a group of points.
                        //// Get points within bounds of dragged rectangle
                        //if (_shiftDown)
                        //{
                        //    List<DataPoint> tempPoints = GetSelectedPoints();
                        //    foreach (DataPoint pt in tempPoints)
                        //    {
                        //        if (!SelectedPoints.Contains(pt))
                        //        {
                        //            SelectedPoints.Add(pt);
                        //            pt.Selected = true;
                        //        }
                        //    }
                        //}
                        //else
                        //{
                        //    SelectedPoints = GetSelectedPoints();
                        //}

                        // Reset status.
                        _dragging = false;
                        _dragCursor = null;

                        // Force repaint of chart
                        Invalidate();
                        Refresh();

                        FireScaleChange();

                        _startMousePos = new Point();
                        _endMousePos = new Point();
                    }
                    else if (_shiftDown)
                    {
                        DataPoint pt = GetClosestPoint(new Point(e.X, e.Y));

                        if (pt != null)
                        {
                            if (SelectedPoints.Contains(pt))
                            {
                                SelectedPoints.Remove(pt);
                                pt.Selected = false;
                            }
                            else
                            {
                                SelectedPoints.Add(pt);
                                pt.Selected = true;
                            }

                            Invalidate();
                            Refresh();
                        }
                    }
                    else if (_dragCursor != null)
                    {
                        // Notify clients.
                        ChartCursorMove(this, new ChartCursorMoveEventArgs() { CursorId = _dragCursor.Id, Position = _dragCursor.Position });
                    }

                    // Reset status.
                    _mouseDown = false;
                    _dragCursor = null;
                    break;
            }
        }

        /// <summary>Sets the _mouseDown state to true.</summary>
        /// <param name="sender">Object that sent triggered the event.</param>
        /// <param name="e">The particular MouseEventArgs (DoubleClick, Click, etc.).</param>
        private void Grid_MouseDown(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    // Retain mouse state.
                    _mouseDown = true;
                    _startMousePos = new Point(e.X, e.Y);

                    if (!_shiftDown)
                    {
                        // Are we on a cursor?
//                        _dragCursor = GetClosestCursor(_startMousePos);

                        // Unselect previously selected points.
                        UnselectPoints();
                        SelectedPoints = new List<DataPoint>();

                        // Force repaint of chart.
                        Invalidate();
                        Refresh();
                    }
                    break;
            }
        }

        /// <summary>If the _mouseDown state is true then move the chart with the mouse.
        /// If the mouse is over a data point, show its coordinates.</summary>
        /// <param name="sender">Object that sent triggered the event.</param>
        /// <param name="e">The particular MouseEventArgs (DoubleClick, Click, etc.).</param>
        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            // Focus();
            if (!_ctrlDown)
            {
                Point newPos = new Point(e.X, e.Y);

                // If the _mouseDown state is true then move the chart with the mouse.
                if (_mouseDown && (LastMousePos != new Point(int.MaxValue, int.MaxValue)))
                {
                    int xChange = (newPos.X - LastMousePos.X);
                    int yChange = (newPos.Y - LastMousePos.Y);

                    // If there is a change in x or y...
                    if ((xChange + yChange) != 0)
                    {
                        if (_dragCursor != null)
                        {
                            // Update the cursor position. Get mouse x and convert to x axis scaled value.
                            PointF pt = GetChartPoint(new PointF(newPos.X, newPos.Y));
                            _dragCursor.Position = pt.X;
                        }
                        else
                        {
                            // Adjust the axes
                            _origin.Y += yChange;
                            _origin.X += xChange;

                            FireScaleChange();
                        }

                        // Repaint
                        Invalidate();
                        Refresh();
                    }
                }

                // If the mouse is over a point or cursor, show its tooltip.
                if (!_mouseDown && newPos != LastMousePos) // Apparently a known issue is that showing a tooltip causes a MouseMove event to get generated.
                {
                    DataPoint closestPoint = GetClosestPoint(newPos);
 //                   ChartCursor closestCursor = CursorToolTipProvider != null ? GetClosestCursor(newPos) : null;

                    if (closestPoint != null)
                    {
                        if (!closestPoint.Hide || _showHidden)
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append("Y:" + closestPoint.YString + "\nX:" + closestPoint.XString);

                            if (ChartToolTipProvider != null)
                            {
                                sb.Append(Environment.NewLine + ChartToolTipProvider(closestPoint.Id));
                            }

                            if (DPToolTipProvider != null)
                            {
                                string retval = DPToolTipProvider(closestPoint);
                                if (retval != "")
                                {
                                    sb.Append(Environment.NewLine + retval);
                                }
                            }

                            // Display the tooltip
                            toolTip.Show(sb.ToString(), this, newPos.X + 15, newPos.Y);
                        }
                    }
                    //else if (closestCursor != null)
                    //{
                    //    // Display the tooltip
                    //    string s = CursorToolTipProvider(closestCursor.Id);
                    //    if (s.Length > 0)
                    //    {
                    //        toolTip.Show(s, this, newPos.X + 15, newPos.Y);
                    //    }
                    //    else
                    //    {
                    //        toolTip.Hide(this);
                    //    }
                    //}
                    else
                    {
                        // Hide the tooltip
                        toolTip.Hide(this);
                    }
                }

                LastMousePos = newPos;
            }
            else
            {
                if (_mouseDown)
                {
                    // Do some special stuff to show a rectangle selection box.
                    _endMousePos = new Point(e.X, e.Y);
                    _dragging = true;

                    // Force repaint of chart
                    Invalidate();
                    Refresh();
                }
            }
        }

        /// <summary>Handler for getting user scale values.</summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event Arguments</param>
        private void Grid_MouseClick(object sender, MouseEventArgs e)
        {
            if (e != null && e.Button == MouseButtons.Right && !SuppressEvents && _data.Count > 0)
            {
                ContextMenuStrip cms = new ContextMenuStrip();
                cms.ShowImageMargin = false;
                //                cms.Items.Add("Set Scale", null, new EventHandler(ShowChartScale));
                cms.Show(this, new Point(e.X, e.Y));
            }
        }

        /// <param name="sender">Object that triggered the event.</param>
        /// <param name="e">The Key Event arguments.</param>
        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                ///// basic control //////
                case Keys.ControlKey:
                    if (!_ctrlDown)
                    {
                        _startMousePos = new Point(LastMousePos.X, LastMousePos.Y);
                    }
                    _ctrlDown = true;
                    break;

                case Keys.ShiftKey:
                    _shiftDown = true;
                    break;

                ///// zoom control //////
                case Keys.I:
                    ZoomIn();
                    break;

                case Keys.O:
                    ZoomOut();
                    break;

                ///// zoom x or y axis only /////
                case Keys.Y:
                    _yDown = true;
                    break;

                case Keys.X:
                    _xDown = true;
                    break;

                ///// shift axes with ctrl-arrow //////
                case Keys.Left:
                    _origin.X -= _shiftSpeed;
                    Invalidate();
                    Refresh();
                    break;

                case Keys.Right:
                    _origin.X += _shiftSpeed;
                    Invalidate();
                    Refresh();
                    break;

                case Keys.Up:
                    _origin.Y -= _shiftSpeed;
                    Invalidate();
                    Refresh();
                    break;

                case Keys.Down:
                    _origin.Y += _shiftSpeed;
                    Invalidate();
                    Refresh();
                    break;

                ///// reset //////
                case Keys.H:
                    _firstPaint = true;
                    Invalidate();
                    Refresh();
                    break;
            }
        }

        /// <summary>Reset _xDown or _yDown depending on which one was unpressed.</summary>
        /// <param name="sender">Object that triggered the event.</param>
        /// <param name="e">The Key Event arguments.</param>
        private void Grid_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Y:
                    _yDown = false;
                    break;

                case Keys.X:
                    _xDown = false;
                    break;

                case Keys.ControlKey:
                    _ctrlDown = false;

                    if (_dragging)
                    {
                        GetSelectedPoints();
                        _dragging = false;
                        _dragCursor = null;
                        Invalidate();
                        Refresh();
                    }

                    _startMousePos = new Point();
                    _endMousePos = new Point();
                    break;

                case Keys.ShiftKey:
                    _shiftDown = false;
                    break;
            }
        }

        /// <summary>Select multiple points.</summary>
        /// <returns>The points.</returns>
        private List<DataPoint> GetSelectedPoints()
        {
            List<DataPoint> points = new List<DataPoint>();

            if (!_startMousePos.IsEmpty && !_endMousePos.IsEmpty)
            {
                float xmin, xmax, ymin, ymax;
                if (_startMousePos.X > _endMousePos.X)
                {
                    xmin = _endMousePos.X;
                    xmax = _startMousePos.X;
                }
                else
                {
                    xmin = _startMousePos.X;
                    xmax = _endMousePos.X;
                }

                if (_startMousePos.Y > _endMousePos.Y)
                {
                    ymin = _endMousePos.Y;
                    ymax = _startMousePos.Y;
                }
                else
                {
                    ymin = _startMousePos.Y;
                    ymax = _endMousePos.Y;
                }

                foreach (DataPoint point in _data)
                {
                    if (point.ClientPoint.X >= xmin && point.ClientPoint.X <= xmax &&
                        point.ClientPoint.Y >= ymin && point.ClientPoint.Y <= ymax)
                    {
                        points.Add(point);
                        point.Selected = true;
                    }
                }
            }

            return points;
        }

        /// <summary>Unselect multiple points.</summary>
        private void UnselectPoints()
        {
            foreach (DataPoint point in SelectedPoints)
            {
                point.Selected = false;
            }
        }

        #region Draw Axes and Labels
        /// <summary>Draw the axes on the chart.</summary>
        /// <param name="g">The Graphics object to use.</param>
        /// <param name="xticks">List of X Tick values.</param>
        /// <param name="yticks">List of Y Tick values.</param>
        private void DrawGrid(Graphics g, out List<StringFloatPair> xticks, out List<StringFloatPair> yticks)
        {
            xticks = new List<StringFloatPair>();
            yticks = new List<StringFloatPair>();

            // Draw X-Axis Increments
            float incX = _xMinScale - _xMinScale % _xTickVal;
            if (_xMinScale != _xMaxScale)
            {
                StringBuilder sb = new StringBuilder();
                Pen penGrid = new Pen(GridLineColor);
                for (; incX <= _xMaxScale; incX += _xTickVal)
                {
                    if (incX > _xMinScale)
                    {
                        sb.Append("TODO");// SeriesCollection[0].XAxisTickLabel(incX));
                        float tickPos = _dataRect.Left + (incX - _xMinScale) * _xZoomedScale;
                        xticks.Add(new StringFloatPair(sb.ToString(), tickPos));
                        sb.Length = 0;

                        g.DrawLine(penGrid, tickPos, 2.0f, tickPos, _dataRect.Bottom - 2.0f);
                    }
                }
            }

            // Draw Y-Axis Increments above X-axis
            float incY = _yMinScale - _yMinScale % _yTickVal;
            if (_yMinScale != _yMaxScale)
            {
                StringBuilder sb = new StringBuilder();
                Pen penGrid = new Pen(GridLineColor);
                for (; incY <= _yMaxScale; incY += _yTickVal)
                {
                    if (incY > _yMinScale)
                    {
                        sb.Append("TODO");// sb.Append(SeriesCollection[0].YAxisTickLabel(incY));
                        float tickPos = _dataRect.Bottom - (incY - _yMinScale) * _yZoomedScale;
                        yticks.Add(new StringFloatPair(sb.ToString(), tickPos));
                        sb.Length = 0;

                        g.DrawLine(penGrid, _dataRect.Left + 2.0f, tickPos, Right - 23.0f, tickPos);
                    }
                }
            }
        }

        /// <summary>Overwrites non viewable region with a set of white rectangles.</summary>
        /// <param name="g">Graphics object</param>
        private void ClearNonViewableRegion(Graphics g)
        {
            Brush br = Brushes.White;
            g.FillRectangle(br, 1, 1, _dataRect.Left - 1, Height);
            g.FillRectangle(br, 1, _dataRect.Bottom, Width - 1, Bottom - _dataRect.Bottom);
        }

        /// <summary>Draws tick values</summary>
        /// <param name="g">Graphics object</param>
        /// <param name="xticks">List of X Tick values</param>
        /// <param name="yticks">List of Y Tick values</param>
        private void DrawTicks(Graphics g, List<StringFloatPair> xticks, List<StringFloatPair> yticks)
        {
            // Make a pen with the proper color and size for drawing the axes.
            Pen pen = new Pen(AxesColor, AxesWidth);

            // Draw axis lines.
            g.DrawLine(pen, _dataRect.Left, _dataRect.Bottom, _dataRect.Right, _dataRect.Bottom);
            g.DrawLine(pen, _dataRect.Left, _dataRect.Top, _dataRect.Left, _dataRect.Bottom);

            // Draw X-Axis Increments
            foreach (StringFloatPair pr in xticks)
            {
                // offset for multi-line labels
                int offset = pr.text.Contains("\n") ? 2 * pr.text.Split(new char[] { '\n' })[1].Length : 2 * pr.text.Length;
                float labelPos = pr.value - offset;
                if (XLabelsVisible)
                {
                    DrawLabel(g, labelPos, _dataRect.Bottom + TICK_SIZE, pr.text, _tickFont, 0);
                }
                g.DrawLine(pen, pr.value, _dataRect.Bottom, pr.value, _dataRect.Bottom + TICK_SIZE);
            }

            // Draw Y-Axis Increments above X-axis
            foreach (StringFloatPair pr in yticks)
            {
                // Determine Font width info for adjusting position.
                float width = pr.text.Length * (_tickFont.Size - 2) + 3f;
                if (YLabelsVisible)
                {
                    DrawLabel(g, _dataRect.Left - TICK_SIZE - width, pr.value - _tickFont.GetHeight() / 2f, pr.text, _tickFont, 0);
                }
                g.DrawLine(pen, _dataRect.Left - TICK_SIZE, pr.value, _dataRect.Left, pr.value);
            }
        }

        /// <summary>An axis label should be drawn using the specified transformations.</summary>
        /// <param name="g">The Graphics object to use.</param>
        /// <param name="transformX">The X transform</param>
        /// <param name="transformY">The Y transform</param>
        /// <param name="labelText">The text of the label.</param>
        /// <param name="font">Font to use.</param>
        /// <param name="rotationDegrees">The rotation of the axis.</param>
        private void DrawLabel(Graphics g, float transformX, float transformY, string labelText, Font font, int rotationDegrees)
        {
            g.TranslateTransform(transformX, transformY);
            g.RotateTransform(rotationDegrees);
            g.DrawString(labelText, font, Brushes.Black, new PointF(0f, 0f));
            g.ResetTransform();
        }
        #endregion


        #region Zoom
        /// <summary>Zoom in the charting control.</summary>
        public void ZoomIn()
        {
            if (_data.Count > 0)
            {
                float oldXScale = _xZoomFactor;
                float oldYScale = _yZoomFactor;

                bool shouldZoom = true;

                if (!_yDown)
                {
                    {
                        _xZoomFactor *= _zoomSpeed;

                        // Prevent shifting when zoom reaches limit
                        if (_xZoomFactor > MAX_ZOOM_LIMIT_OTHER)// && SeriesCollection[0].XType != typeof(DateTime))
                        {
                            _xZoomFactor = MAX_ZOOM_LIMIT_OTHER;
                        }
                    }
                }
                if (!_xDown && shouldZoom)
                {
                    {
                        _yZoomFactor *= _zoomSpeed;

                        // Prevent shifting when zoom reaches limit
                        if (_yZoomFactor > MAX_ZOOM_LIMIT_OTHER)// && SeriesCollection[0].XType != typeof(DateTime))
                        {
                            _yZoomFactor = MAX_ZOOM_LIMIT_OTHER;
                        }
                    }
                }

                if (shouldZoom)
                {
                    _zoomed = true;
                    _xZoomedScale = _xActualScale * _xZoomFactor;
                    _yZoomedScale = _yActualScale * _yZoomFactor;
                    Recenter(_xZoomFactor / oldXScale, _yZoomFactor / oldYScale);
                }
                else
                {
                    _xZoomFactor = oldXScale;
                    _yZoomFactor = oldYScale;
                }
            }
        }

        /// <summary>Zoom out the charting control.</summary>
        public void ZoomOut()
        {
            if (_data.Count > 0)
            {
                float oldXScale = _xZoomFactor;
                float oldYScale = _yZoomFactor;

                bool shouldZoom = true;

                if (!_yDown)
                {
                    if (_xZoomFactor <= MIN_ZOOM_LIMIT)
                    {
                        shouldZoom = false;
                    }
                    else
                    {
                        _xZoomFactor /= _zoomSpeed;

                        if (_xZoomFactor < MIN_ZOOM_LIMIT)
                        {
                            _xZoomFactor = MIN_ZOOM_LIMIT;
                        }
                    }
                }
                if (!_xDown && shouldZoom)
                {
                    if (_yZoomFactor <= MIN_ZOOM_LIMIT)
                    {
                        //_xZoomFactor = oldXScale;
                        shouldZoom = false;
                    }
                    else
                    {
                        _yZoomFactor /= _zoomSpeed;

                        if (_yZoomFactor < MIN_ZOOM_LIMIT)
                        {
                            _yZoomFactor = MIN_ZOOM_LIMIT;
                        }
                    }
                }

                if (shouldZoom)
                {
                    _zoomed = true;
                    _xZoomedScale = _xActualScale * _xZoomFactor;
                    _yZoomedScale = _yActualScale * _yZoomFactor;
                    Recenter(_xZoomFactor / oldXScale, _yZoomFactor / oldYScale);
                }
                else
                {
                    _xZoomFactor = oldXScale;
                    _yZoomFactor = oldYScale;
                }
            }
        }

        /// <summary>Recenter the chart after zooming in or out.</summary>
        /// <param name="xRatio">The change ratio for the x axis</param>
        /// <param name="yRatio">The change ratio for the y axis</param>
        private void Recenter(float xRatio, float yRatio)
        {
            // Get the axes positions relative to the center of the control.
            float xAxisPosFromCenter = _origin.Y - Height / 2;
            float yAxisPosFromCenter = _origin.X - Width / 2;

            // Calculate the change in positions.
            float dY = ((xAxisPosFromCenter * yRatio) - xAxisPosFromCenter);
            float dX = ((yAxisPosFromCenter * xRatio) - yAxisPosFromCenter);

            // Set the new x and y origin positions.
            _origin.Y = xAxisPosFromCenter + dY + Height / 2;
            _origin.X = yAxisPosFromCenter + dX + Width / 2;

            Invalidate();
            Refresh();

            FireScaleChange();
        }
        #endregion




        ////// helpers //////
        /// <summary>Obtain the point on the chart based on a client position.</summary>
        /// <param name="p">The PointF to restore</param>
        /// <returns>A PointF corresponding to the true coordinates (non-client) of the given PointF.</returns>
        private PointF GetChartPoint(PointF p)
        {
            return new PointF((float)((p.X - _origin.X) / _xZoomedScale), (float)((_origin.Y - p.Y) / _yZoomedScale));
        }


        /// <summary>Obtain a client point based on a point on the chart.</summary>
        /// <param name="p">The PointF to correct</param>
        /// <returns>A PointF corresponding to the proper raw client position of the given PointF.</returns>
        private PointF GetClientPoint(PointF p)
        {
            bool xPos = (p.X > 0);
            bool yPos = (p.Y > 0);
            float x = p.X * _xZoomedScale;
            float y = p.Y * _yZoomedScale;
            PointF retPoint = new PointF(0f, 0f);

            if (xPos && yPos) // Both Positive
            {
                retPoint = new PointF(x + _origin.X, _origin.Y - y);
            }
            else if (xPos && !yPos) // Y is negative
            {
                retPoint = new PointF(x + _origin.X, _origin.Y + Math.Abs(y));
            }
            else if (!xPos && yPos) // X is negative
            {
                retPoint = new PointF(_origin.X - Math.Abs(x), _origin.Y - y);
            }
            else // Both Negative
            {
                retPoint = new PointF(_origin.X - Math.Abs(x), _origin.Y + Math.Abs(y));
            }

            return retPoint;
        }

        /// <summary>Returns the tick increment value and sets the format specifier</summary>
        /// <param name="tickRng">Starting tick range</param>
        /// <param name="dataRng">Data range</param>
        /// <param name="numTicks">Number of desired tick values</param>
        /// <param name="xAxis">True if x axis</param>
        /// <returns>Tick increment</returns>
        private float GetTickValue(float tickRng, float dataRng, int numTicks, bool xAxis)
        {
            int count = 1;
            while (tickRng < dataRng)
            {
                tickRng = (count / 2) != 0 ? (2.5F * tickRng) : (2 * tickRng);
                count = (count++ / 2) != 0 ? 0 : count;
            }

            if (xAxis)
            {
                //SeriesCollection[0].XFormatSpecifier = GetFormatSpecifier(tickRng, SeriesCollection[0].XType);
            }
            else
            {
                //SeriesCollection[0].YFormatSpecifier = GetFormatSpecifier(tickRng, SeriesCollection[0].YType);
            }

            return tickRng / numTicks;
        }

        // /// <summary>Sets the format specifier based upon the range of data for floats</summary>
        // /// <param name="tickRng">Tick range</param>
        // /// <param name="axisType">The datatype for the axis</param>
        // /// <returns>Format specifier</returns>
        // private string GetFormatSpecifier(float tickRng, Type axisType)
        // {
        //     string format = "";

        //     if (axisType.ToString() == "System.Double")
        //     {
        //         if (tickRng >= 100)
        //         {
        //             format = "0;-0;0";
        //         }
        //         else if (tickRng < 100 && tickRng >= 10)
        //         {
        //             format = "0.0;-0.0;0";
        //         }
        //         else if (tickRng < 10 && tickRng >= 1)
        //         {
        //             format = "0.00;-0.00;0";
        //         }
        //         else if (tickRng < 1)
        //         {
        //             format = "0.000;-0.000;0";
        //         }
        //     }

        //     return format;
        // }

            
        /// <summary>Draw a ScatterPlot with the supplied graphics and for the designated series.</summary>
        /// <param name="g">The Graphics object to use.</param>
        private void DrawData(Graphics g)
        {
            // The pen and brush to be used for drawing
            Pen pen = new Pen(_dotColor, 1.0f); // .Color, Common.POINT_BORDER_DEFAULT);

            // Iterate through the points in the series, and save them in temp array for charting.
            // Shift the point to proper position on client screen.
            foreach (DataPoint point in _data)
            {
                // Get and save the client position.
                point.ClientPoint = GetClientPoint(point.ScaledPoint);

                if (_dataRect.Contains(point.ClientPoint))
                {
                    if (!point.Hide || _showHidden)
                    {
                        // Select (current) pen color
                        if (point.Selected)
                        {
                            pen.Color = Color.Red;// Globals.Instance.UserSettings.SelectedPointColor;
                        }
                        else
                        {
                            // Restore old pen
                            pen.Color = _dotColor;
                        }


                        // 1/2 of the point width
                        float pointWidth = 50;
                        float x = pointWidth / 2.0f;

                        // Draw Point Shape

                        // Circle is drawn with Position=TopLeft Corner.
                        // Shifting the Circle's position by its radius will place it centered on the line.
                        //RectangleF circleBound = new RectangleF(point.ClientPoint.X - x, point.ClientPoint.Y - x, x * 2, x * 2);
                        //g.DrawEllipse(pen, circleBound);
                        //if (point.PointType == ChartPointType.CircleFilled)
                        //{
                        //    g.FillEllipse(pen.Brush, circleBound);
                        //}

                        RectangleF squareBound = new RectangleF(point.ClientPoint.X - x, point.ClientPoint.Y - x, pointWidth, 5);
                        g.DrawRectangles(pen, new RectangleF[] { squareBound });
                        //if (point.PointType == ChartPointType.SquareFilled)
                        //{
                        //    g.FillRectangle(pen.Brush, squareBound);
                        //}

                        //// Draw border.
                        //if (point.BorderShape != Definitions.ChartPointBorderType.None)
                        //{
                        //    // Determine Border Shape (Around Point)
                        //    switch (point.BorderShape)
                        //    {
                        //        case ChartPointBorderType.Circle:
                        //            g.DrawArc(pen, point.ClientPoint.X - x * 4, point.ClientPoint.Y - x * 4, x * 8, x * 8, 0, 360);
                        //            break;

                        //        case ChartPointBorderType.Triangle:
                        //            g.DrawPolygon(pen, new PointF[]
                        //            {
                        //                new PointF(point.ClientPoint.X - x * 5, point.ClientPoint.Y + x * 3),
                        //                new PointF(point.ClientPoint.X + x * 5, point.ClientPoint.Y + x * 3),
                        //                new PointF(point.ClientPoint.X, point.ClientPoint.Y - x * 5)
                        //            });
                        //            break;

                        //        case ChartPointBorderType.Square:
                        //            g.DrawRectangle(pen, point.ClientPoint.X - x * 3, point.ClientPoint.Y - x * 3, x * 6, x * 6);
                        //            break;
                        //    }
                        //}
                    }
                }
            }
        }



        // /// <summary>Show or hide hidden points.</summary>
        // /// <param name="show">Show hidden points.</param>
        // public void ShowHidden(bool show)
        // {
        //     _showHidden = show;

        //     // Force a re-Paint to update.
        //     Invalidate();
        //     Update();
        // }

        ///// <summary>Clear everything.</summary>
        //public void Clear()
        //{
        //    SeriesCollection = new DataSeriesCollection();
        //    Invalidate();
        //    Refresh();
        //}

        ///// <summary>Client wants to reduce noise.</summary>
        //public void HideTooltip()
        //{
        //    toolTip.Hide(this);
        //}
    }

    /// <summary>Class for storing a string/float pair. TODO Probably should be a tuple.</summary>
    public class StringFloatPair
    {
        public string text = "";
        public float value = 0f;

        /// <summary>Constructor</summary>
        /// <param name="txt">Text value</param>
        /// <param name="val">Float value</param>
        public StringFloatPair(string txt, float val)
        {
            text = txt;
            value = val;
        }
    }

    /// <summary>Class representing one data point and associated information.</summary>
    public class DataPoint
    {
        #region Properties
        /// <summary>The scaled position for the point.</summary>
        public PointF ScaledPoint { get; set; }

        /// <summary>The readable value for the x point.</summary>
        public string XString { get; set; }

        /// <summary>The readable value for the y point.</summary>
        public string YString { get; set; }

        /// <summary>The unique id for this point.</summary>
        public int Id { get; private set; }

        /// <summary>The user tag. This is as .NET uses them, not as OARS did.</summary>
        public object Tag { get; set; }

        /// <summary>For showing/hiding.</summary>
        public bool Hide { get; set; }

        ///<summary>Gets or sets whether the point is filtered out.</summary>
        public bool Filtered { get; set; }

        /// <summary>This point has been selected while in the chart, show a different color.</summary>
        public bool Selected { get; set; }

        /// <summary>Storage in client coordinates for use by client.</summary>
        public PointF ClientPoint { get; set; }
        #endregion

        #region Constructors
        /// <summary>Normal constructor.</summary>
        /// <param name="scaledPoint">The point after scaling.</param>
        /// <param name="xString">The string for the x label</param>
        /// <param name="yString">The string for the y label</param>
        /// <param name="id">The id of the DataPoint</param>
        public DataPoint(PointF scaledPoint, string xString, string yString, int id)
        {
            ScaledPoint = scaledPoint;
            XString = xString;
            YString = yString;
            Id = id;

            Hide = false;
            Selected = false;
            Filtered = false;
            ClientPoint = new PointF();
        }

        /// <summary>Default constructor.</summary>
        private DataPoint() { }
        #endregion
    }
}
