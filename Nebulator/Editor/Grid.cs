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
using Nebulator.Common;

// TODO2 Get all this working, or at least figure out what it's supposed to do...


namespace Nebulator.Editor
{
    public partial class Grid : UserControl
    {
        // Polygon editing borrowed from http://csharphelper.com/blog/2014/10/let-the-user-draw-polygons-move-them-and-add-points-to-them-in-c/

        /// <summary>Container for info about one tick mark.</summary>
        struct AxisTick
        {
            public string text;
            public float value;
            public AxisTick(string txt, float val)
            {
                text = txt;
                value = val;
            }
        }

        /// <summary>Class for storing info about an X axis cursor. Just float right now.</summary>
        class GridCursor
        {
            /// <summary>The unique ID number associated with this cursor.</summary>
            public int Id { get; set; }

            /// <summary>The color to be used when drawing this cursor.</summary>
            public Color Color { get; set; }

            /// <summary>Gets or sets the line type.</summary>
            public DashStyle LineType { get; set; }

            /// <summary>The width to be used when drawing this cursor.</summary>
            public float LineWidth { get; set; }

            /// <summary>Where it's located.</summary>
            public float Position { get; set; }

            /// <summary>Ensure unique id numbers.</summary>
            static int _nextId = 1;

            /// <summary>Default constructor.</summary>
            public GridCursor()
            {
                Id = _nextId++;
                Color = Color.Blue;
                LineType = DashStyle.Solid;
                LineWidth = 1.5f;
                Position = 0.0f;
            }
        }

        enum DrawState
        {
            Idle,
            ///// These match the original handlers in poly edit.
            /// <summary>Move the next point in the new polygon.</summary>
            MouseMove_Drawing,
            /// <summary>Move the selected corner.</summary>
            MouseMove_MovingCorner,
            /// <summary>Move the selected polygon.</summary>
            MouseMove_MovingPolygon,
            /// <summary>Finish moving the selected corner.</summary>
            MouseUp_MovingCorner,
            /// <summary>Finish moving the selected polygon.</summary>
            MouseUp_MovingPolygon,

            ///// Original grid states
            /// <summary>Dragging the whole grid.</summary>
            MouseMove_DraggingGrid,
            /// <summary>Dragging the mouse to select items.</summary>
            MouseMove_Selecting,
            /// <summary>Dragging a cursor.</summary>
            MouseMove_DraggingCursor
        }

        // in order of priority
        enum PointLocation
        {
            OverCorner, OverEdge, OverPolygon, NotPertinent
        }

        /// <summary>General purpose container for results.</summary>
        class EditResult
        {
            public PointLocation ploc = PointLocation.NotPertinent;
            public PolygonF hitPolygon = null;
            public int hitPoint = -1;
        }


        /// <summary>What we be doing.</summary>
        DrawState _drawState = DrawState.Idle;

        #region important old fields +++++++++++++++++++++++++++++++++++++++
        /// <summary>Current mouse position.</summary>
        Point _startMousePos = new Point(int.MaxValue, int.MaxValue);

        /// <summary>Saves the previous mouse position.</summary>
        Point _lastMousePos = new Point(int.MaxValue, int.MaxValue);
        
        /// <summary>End mouse position.</summary>
        Point _endMousePos = new Point(int.MaxValue, int.MaxValue);

        /// <summary>Init flag.</summary>
        bool _firstPaint = true;

        /// <summary>Whether the chart zoom has changed.</summary>
        bool _zoomed = false;

        ///// <summary>Are hidden points visible.</summary>
        //bool _showHidden = true;

        /// <summary>The area to draw the info.</summary>
        RectangleF _dataRect = new RectangleF();

        /// <summary>Logical data origin.</summary>
        PointF _origin = new PointF();
        #endregion

        #region Constants
        /// <summary>Length of the tick marks in pixels.</summary>
        const int TICK_SIZE = 5;

        /// <summary>Maximum zoom in limit.</summary>
        const int MAX_ZOOM_LIMIT_OTHER = 100;

        /// <summary>Minimum zoom out limit.</summary>
        const float MIN_ZOOM_LIMIT = 0.1f;

        /// <summary>How close do you have to be to select a feature.</summary>
        const int MOUSE_SELECT_RANGE = 5;

        /// <summary></summary>
        const float LINE_WIDTH = 0.1f;
        #endregion


        #region Fields - Grid Cursors
        /// <summary>Current cursor set.</summary>
        private List<GridCursor> _gridCursors = new List<GridCursor>();

        /// <summary>If non-null identifies the grid cursor being dragged now.</summary>
        private GridCursor _dragGridCursor = null;
        #endregion

        #region Fields - Zooming and shift
        ///// <summary>Speed at which to shift the control (left/right/up/down).</summary>
        //int _shiftSpeed = 5;

        /// <summary>Speed at which to zoom in/out.</summary>
        float _zoomSpeed = 1.1f; // 1.25f is too fast

        /// <summary>Current X scale factor based on _xZoomFactor.</summary>
        float _xZoomedScale = float.MaxValue;

        /// <summary>Current Y scale factor based on _yZoomFactor.</summary>
        float _yZoomedScale = float.MaxValue;

        /// <summary>Current user selected zoom factor.</summary>
        float _xZoomFactor = 1.0f;

        /// <summary>Current user selected zoom factor.</summary>
        float _yZoomFactor = 1.0f;
        #endregion

        #region Fields - Grid
        /// <summary>Default x tick value.</summary>
        float _xTickVal = 1;

        /// <summary>Default y tick value.</summary>
        float _yTickVal = 1;

        /// <summary>Font to be used when drawing the labels.</summary>
        Font _axisLabelFont = new Font("Arial", 9, FontStyle.Bold);

        /// <summary>Font to be used when drawing the labels.</summary>
        Font _titleLabelFont = new Font("Arial", 11, FontStyle.Bold);

        /// <summary>Font to be used when drawing the text tick labels.</summary>
        Font _tickFont = new Font("Arial", 7);
        #endregion

        #region Fields - Scales
        /// <summary>X min in data units.</summary>
        float _xMinScale = float.MinValue;

        /// <summary>X max in data units.</summary>
        float _xMaxScale = float.MaxValue;

        /// <summary>Y min in data units.</summary>
        float _yMinScale = float.MinValue;

        /// <summary>Y max in data units.</summary>
        float _yMaxScale = float.MaxValue;

        /// <summary>X Scale of data range vs viewable chart range</summary>
        float _xActualScale = float.MaxValue;

        /// <summary>Y Scale of data range vs viewable chart range</summary>
        float _yActualScale = float.MaxValue;
        #endregion

        #region Properties - Labels
        /// <summary>The title label.</summary>
        [Category("Labels")]
        [Description("Title Label")]
        public string TitleLabel { get; set; } = "Title";

        /// <summary>The X axis label.</summary>
        [Category("Labels")]
        [Description("X Axis Label")]
        public string XAxisLabel { get; set; } = "X Axis";

        /// <summary>The Y axis label.</summary>
        [Category("Labels")]
        [Description("Y Axis Label")]
        public string YAxisLabel { get; set; } = "Y Axis";
        #endregion

        #region Properties - Axes
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

        #region Properties - Colors
        /// <summary>Color to be used when drawing the gridlines.</summary>
        [Category("Grid")]
        [Description("Gridline color")]
        public Color GridLineColor { get; set; } = Color.LightGray;

        /// <summary>Color to be used when drawing the background of the chart.</summary>
        [Category("Grid")]
        [Description("Background Color of Chart")]
        public Color GridBackColor { get; set; } = Color.White;
        #endregion

        #region Events
        ///// <summary>Scale change event class.</summary>
        //public class ScaleChangeEventArgs : EventArgs
        //{
        //    public float XMinScale { get; set; } = float.MinValue;
        //    public float XMaxScale { get; set; } = float.MaxValue;
        //    public float YMinScale { get; set; } = float.MinValue;
        //    public float YMaxScale { get; set; } = float.MaxValue;
        //}

        ///// <summary>Publish event for scale change.</summary>
        //public event EventHandler<ScaleChangeEventArgs> ScaleChangeEvent;

        ///// <summary>Fire the event for scale change.</summary>
        //void FireScaleChangeEvent()
        //{
        //    ScaleChangeEvent?.Invoke(this, new ScaleChangeEventArgs()
        //    {
        //        XMinScale = _xMinScale,
        //        XMaxScale = _xMaxScale,
        //        YMinScale = _yMinScale,
        //        YMaxScale = _yMaxScale
        //    });
        //}


        /// <summary>Get tooltip info from client.</summary>
        public class ToolTipEventArgs : EventArgs
        {
            // Info supplied to client.
            public PolygonF Data { get; set; } = null;
            public int CursorId { get; set; } = 0;
            public float Position { get; set; } = 0;
            // Info filled in by client.
            public string Text { get; set; } = "";
        }
        public event EventHandler<ToolTipEventArgs> ToolTipEvent;
        #endregion



        ////////////////////// my new stuff ////////////////////////////
        List<float> _xVals = new List<float>();
        List<float> _yVals = new List<float>();
        float _xRange = 0;
        float _yRange = 0;

        public List<PolygonF> Polygons { get; set; } = new List<PolygonF>();


        ////////////////////// from poly editor ////////////////////////////
        
        // A new polygon being added.
        PolygonF _newPolygon = null;

        // Point being added to a new polygon.
        PointF _newPoint = new PointF();

        // The polygon being moved.
        PolygonF _movingPolygon = null;

        // The index of the corner being moved.
        int _movingCorner = -1;

        // Remember the offset from the mouse to the point for later use.
        float _offsetX = float.NaN;
        // Remember the offset from the mouse to the point for later use.
        float _offsetY = float.NaN;

        // The "size" of an object for mouse over purposes.
        const float TARGET_SIZE = 3;





        #region Lifecycle
        public Grid()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            InitializeComponent();
        }

        void Grid_Load(object sender, EventArgs e)
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
            Geometry.HitRange = TARGET_SIZE;

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

        }
        #endregion

        #region Public methods
        /// <summary>Constructs a new grid and initializes the collection.</summary>
        public void InitData()
        {
            _firstPaint = true;

            //_xVals = points.Select(i => i.X).ToList();
            //_yVals = points.Select(i => i.Y).ToList();
            //_xRange = _xVals.Max() - _xVals.Min();
            //_yRange = _yVals.Max() - _yVals.Min();


            //// Set the default scales.
            //_xMinScale = _xVals.Min();
            //_xMaxScale = _xVals.Max();
            //_yMinScale = _yVals.Min();
            //_yMaxScale = _yVals.Max();


            Invalidate(); // :::::::::::::::: or Refresh()??
            //https://blogs.msdn.microsoft.com/subhagpo/2005/02/22/whats-the-difference-between-control-invalidate-control-update-and-control-refresh/
            //Invalidate() simply adds a region to the update region of the control. The next time WM_PAINT is received, the area you invalidated plus any other invalidated regions, are marked for painting. When RedrawWindow() is called, that will normally post a WM_PAINT message to the application queue. The system is free to do what it wants with that, usually more pressing business, and paint when it can.
            //If you call Update(), you get GDI + 's UpdateWindow() which won't mark a region for repainting, but pushes a WM_PAINT directly to WNDPROC(), bypassing the application queue.
            //If you need an immediate refresh of a control, use Refresh(), which invalidates the region then immediately calls Update().
        }

        ///// <summary>Constructs a new grid and initializes the collection.</summary>
        ///// <param name="points">A collection data points</param>
        //public void InitData_old(List<PointF> points)
        //{
        //    int pi = 1;

        //    _polygons.Clear();

        //    //foreach (PointF pt in points)
        //    //{
        //    //    _data.Add(new PolygonF DataPoint(pt, "PX", "PY", pi++));
        //    //}

        //    _xVals = points.Select(i => i.X).ToList();
        //    _yVals = points.Select(i => i.Y).ToList();
        //    _xRange = _xVals.Max() - _xVals.Min();
        //    _yRange = _yVals.Max() - _yVals.Min();

        //    _firstPaint = true;

        //    // Set the default scales.
        //    _xMinScale = _xVals.Min();
        //    _xMaxScale = _xVals.Max();
        //    _yMinScale = _yVals.Min();
        //    _yMaxScale = _yVals.Max();
        //}

        #endregion

        #region Drawing
        /// <summary>
        /// Main drawing function. For fancier stuff see the OOARS code.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.Clear(GridBackColor);

                // If first paint reset zoom scales.
                if (_firstPaint)
                {
                    InitGrid();
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
                List<AxisTick> xticks;
                List<AxisTick> yticks;
                DrawGrid(e.Graphics, out xticks, out yticks);
                // Draw the ticks
                DrawTicks(e.Graphics, xticks, yticks);


                // Draw the ChartType specified for each series in the collection.
                ///////DrawData(e.Graphics);
                DrawPolygons(e.Graphics);

                // Draw Cursors
                foreach (GridCursor cursor in _gridCursors)
                {
                    DrawCursor(e.Graphics, cursor);
                }

                //// Clear region outside axes.
                //ClearNonViewableRegion(e.Graphics);


                // Draw labels if supplied.
                int space = 3;

                float size = e.Graphics.MeasureString(TitleLabel, _titleLabelFont).Width;
                Point newLoc = new Point(Width / 2 - (int)size / 2, space);
                DrawLabel(e.Graphics, newLoc.X, newLoc.Y, TitleLabel, _titleLabelFont, 0);

                size = e.Graphics.MeasureString(XAxisLabel, _axisLabelFont).Width;
                newLoc = new Point(Width / 2 - (int)size / 2, Height - _axisLabelFont.Height - space);
                DrawLabel(e.Graphics, newLoc.X, newLoc.Y, XAxisLabel, _axisLabelFont, 0);

                size = e.Graphics.MeasureString(YAxisLabel, _axisLabelFont).Width;
                float theight = _axisLabelFont.Height;
                DrawLabel(e.Graphics, space, Height / 2 + size / 2, YAxisLabel, _axisLabelFont, 270);

                // Draw Selection Rectangle if ctrl is down and not dragging cursor. ::::::::::::::::
                if (KeyboardState.IsKeyDown(Keys.ControlKey) && _drawState == DrawState.MouseMove_Selecting)  // _dragging && _dragCursor == null)
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

                // Draw the Border.
                e.Graphics.DrawRectangle(new Pen(Color.RoyalBlue, 1), new Rectangle(0, 0, Width - 1, Height - 1));

                _firstPaint = false;
            }
            catch
            {
                e.Graphics.DrawLine(new Pen(Color.Red), new Point(0, 0), new Point(Right, Bottom));
                e.Graphics.DrawLine(new Pen(Color.Red), new Point(Right, 0), new Point(0, Bottom));
            }
        }
        #endregion

        #region UI event handlers
        /// <summary>Process depending on state.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Grid_MouseDown(object sender, MouseEventArgs e)
        {
            switch (_drawState)
            {
                case DrawState.MouseMove_Drawing:
                    // We are already drawing a polygon. If it's the right mouse button, finish this polygon.
                    if (e.Button == MouseButtons.Right)
                    {
                        // Finish this polygon.
                        if (_newPolygon.Points.Count > 2)
                        {
                            Polygons.Add(_newPolygon);
                        }
                        _newPolygon = null;

                        // We no longer are drawing.
                        _drawState = DrawState.Idle;
                    }
                    else
                    {
                        // Add a point to this polygon.
                        if (_newPolygon.Points[_newPolygon.Points.Count - 1] != e.Location)
                        {
                            _newPolygon.Add(e.Location);
                        }
                    }
                    break;

                case DrawState.Idle:
                    EditResult eres = WhereIsPoint(e.Location);

                    switch (eres.ploc)
                    {
                        case PointLocation.OverCorner:
                            // Start dragging this corner.
                            _drawState = DrawState.MouseMove_MovingCorner;

                            // Remember the polygon and point number.
                            _movingPolygon = eres.hitPolygon;
                            _movingCorner = eres.hitPoint;

                            // Remember the offset from the mouse to the point.
                            _offsetX = eres.hitPolygon.Points[eres.hitPoint].X - e.X;
                            _offsetY = eres.hitPolygon.Points[eres.hitPoint].Y - e.Y;

                            break;


                        case PointLocation.OverEdge:
                            // Add a point.
                            eres.hitPolygon.InsertVertex(eres.hitPoint + 1, e.Location);
                            break;

                        case PointLocation.OverPolygon:
                            // Start moving this polygon.
                            _drawState = DrawState.MouseMove_MovingPolygon;

                            // Remember the polygon.
                            _movingPolygon = eres.hitPolygon;

                            // Remember the offset from the mouse to the segment's first point.
                            _offsetX = eres.hitPolygon.Points[0].X - e.X;
                            _offsetY = eres.hitPolygon.Points[0].Y - e.Y;
                            break;

                        case PointLocation.NotPertinent:
                            if (e.Button == MouseButtons.Left)
                            {
                                // Retain mouse state.
                                _startMousePos = new Point(e.X, e.Y);

                                if (!KeyboardState.IsKeyDown(Keys.ShiftKey))
                                {
                                    // Are we on a cursor?
                                    _dragGridCursor = GetClosestCursor(_startMousePos);

                                    if (_dragGridCursor == null)
                                    {
                                        _drawState = DrawState.MouseMove_DraggingGrid;
                                    }
                                    else
                                    {
                                        _drawState = DrawState.MouseMove_DraggingCursor;
                                    }

                                    // Unselect previously selected points.
                                    //UnselectPoints();
                                    //SelectedPoints = new List<DataPoint>();
                                }
                            }
                            else
                            {
                                // Start a new polygon.
                                _newPolygon = new PolygonF();
                                _newPoint = e.Location;
                                _newPolygon.Add(e.Location);

                                // Get ready to work on the new polygon.
                                _drawState = DrawState.MouseMove_Drawing;
                            }

                            break;
                    }




                    //if (PointIsOverCorner(e.Location, out PolygonF hitPolygon, out int hitPoint))
                    //{
                    //    // Start dragging this corner.
                    //    _drawState = DrawState.MouseMove_MovingCorner;

                    //    // Remember the polygon and point number.
                    //    _movingPolygon = hitPolygon;
                    //    _movingPoint = hitPoint;

                    //    // Remember the offset from the mouse to the point.
                    //    _offsetX = hitPolygon.Points[hitPoint].X - e.X;
                    //    _offsetY = hitPolygon.Points[hitPoint].Y - e.Y;
                    //}
                    //else if (PointIsOverEdge(e.Location, out hitPolygon, out hitPoint, out int hitPoint2, out PointF closestPoint))
                    //{
                    //    // Add a point.
                    //    hitPolygon.InsertVertex(hitPoint + 1, closestPoint);
                    //}
                    //else if (PointIsOverPolygon(e.Location, out hitPolygon))
                    //{
                    //    // Start moving this polygon.
                    //    _drawState = DrawState.MouseMove_MovingPolygon;

                    //    // Remember the polygon.
                    //    _movingPolygon = hitPolygon;

                    //    // Remember the offset from the mouse to the segment's first point.
                    //    _offsetX = hitPolygon.Points[0].X - e.X;
                    //    _offsetY = hitPolygon.Points[0].Y - e.Y;
                    //}
                    //else if (e.Button == MouseButtons.Left)
                    //{
                    //    // Retain mouse state.
                    //    _startMousePos = new Point(e.X, e.Y);

                    //    if (!KeyboardState.IsKeyDown(Keys.ShiftKey))
                    //    {
                    //        // Are we on a cursor?
                    //        _dragCursor = GetClosestCursor(_startMousePos);

                    //        if(_dragCursor == null)
                    //        {
                    //            _drawState = DrawState.MouseMove_DraggingGrid;
                    //        }
                    //        else
                    //        {
                    //            _drawState = DrawState.MouseMove_DraggingCursor;
                    //        }

                    //        // Unselect previously selected points.
                    //        //UnselectPoints();
                    //        //SelectedPoints = new List<DataPoint>();
                    //    }
                    //}
                    //else
                    //{
                    //    // Start a new polygon.
                    //    _newPolygon = new PolygonF();
                    //    _newPoint = e.Location;
                    //    _newPolygon.Add(e.Location);

                    //    // Get ready to work on the new polygon.
                    //    _drawState = DrawState.MouseMove_Drawing;
                    //}
                    break;

                default:
                    // Shouldn't get others.
                    MessageBox.Show($"Grid_MouseDown() got {_drawState}");
                    break;
            }
        }

        /// <summary>Process depending on state.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Grid_MouseUp(object sender, MouseEventArgs e)
        {
            switch(_drawState)
            {
                case DrawState.MouseMove_MovingCorner:
                case DrawState.MouseMove_MovingPolygon:
                    // Finish the op.
                    _drawState = DrawState.Idle;
                    break;

                case DrawState.MouseMove_Selecting:
                    // Set chart to selection area. Must update zoom values too.
                    // Maybe use Recenter()

                    ////// This is the original OOARS behavior: to select a group of points.
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
                    //FireScaleChangeEvent();

                    _drawState = DrawState.Idle;

                    _startMousePos = new Point();
                    _endMousePos = new Point();
                    break;

                case DrawState.MouseMove_DraggingCursor:
                    // Notify clients.
                    //ChartCursorMove(this, new ChartCursorMoveEventArgs() { CursorId = _dragCursor.Id, Position = _dragCursor.Position });
                    _drawState = DrawState.Idle;
                    break;

                case DrawState.MouseMove_DraggingGrid:
                    // Ignore.
                    break;

                default:
                    // Shouldn't get others.
                    MessageBox.Show($"Grid_MouseUp() got {_drawState}");
                    break;
            }
        }

        /// <summary>Process depending on state.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            bool inval = false;
            Point newPos = new Point(e.X, e.Y);

            switch (_drawState)
            {
                case DrawState.MouseMove_Drawing:
                    // Move the next point in the new polygon.
                    _newPoint = e.Location;
                    inval = true;
                    break;

                case DrawState.MouseMove_MovingCorner:
                    // Move the selected corner.
                    _movingPolygon.Points[_movingCorner] = new PointF(e.X + _offsetX, e.Y + _offsetY);
                    inval = true;
                    break;

                case DrawState.MouseMove_MovingPolygon:
                    // Move the selected polygon.
                    // See how far the first point will move.
                    float new_x1 = e.X + _offsetX;
                    float new_y1 = e.Y + _offsetY;

                    float dx = new_x1 - _movingPolygon.Points[0].X;
                    float dy = new_y1 - _movingPolygon.Points[0].Y;

                    if (dx == 0 && dy == 0) return;

                    // Move the polygon.
                    for (int i = 0; i < _movingPolygon.Points.Count; i++)
                    {
                        _movingPolygon.Points[i] = new PointF(_movingPolygon.Points[i].X + dx, _movingPolygon.Points[i].Y + dy);
                    }
                    inval = true;
                    break;

                case DrawState.MouseMove_Selecting:
                    if (MouseButtons == MouseButtons.Left)
                    {
                        // Do some special stuff to show a rectangle selection box.
                        _endMousePos = new Point(e.X, e.Y);
                        inval = true;
                    }
                    break;

                case DrawState.MouseMove_DraggingCursor:
                    inval = true;
                    break;

                case DrawState.MouseMove_DraggingGrid:
                    if ((MouseButtons == MouseButtons.Left) && (_lastMousePos != new Point(int.MaxValue, int.MaxValue)))
                    {
                        int xChange = (newPos.X - _lastMousePos.X);
                        int yChange = (newPos.Y - _lastMousePos.Y);

                        // If there is a change in x or y...
                        if ((xChange + yChange) != 0)
                        {
                            if (_dragGridCursor != null)
                            {
                                // Update the cursor position. Get mouse x and convert to x axis scaled value.
                                PointF pt = GetGridPoint(new PointF(newPos.X, newPos.Y));
                                _dragGridCursor.Position = pt.X;
                            }
                            else
                            {
                                // Adjust the axes
                                _origin.Y += yChange;
                                _origin.X += xChange;

                                // FireScaleChange();
                            }

                            // Repaint
                            Invalidate();
                        }
                    }
                    inval = true;
                    break;

                case DrawState.Idle:
                    // See if we're over a polygon or corner point.
                    Cursor newCursor = Cursors.Cross;
                    EditResult eres = WhereIsPoint(e.Location);

                    switch (eres.ploc)
                    {
                        case PointLocation.OverCorner:
                            newCursor = Cursors.Arrow;
                            break;

                        case PointLocation.OverEdge:
                            newCursor = Cursors.SizeAll; //:::::::::::::::: scrub all cursors.
                            break;

                        case PointLocation.OverPolygon:
                            newCursor = Cursors.Hand;
                            break;

                        case PointLocation.NotPertinent:
                            if ((MouseButtons != MouseButtons.Left) && newPos != _lastMousePos) // Apparently a known issue is that showing a tooltip causes a MouseMove event to get generated.
                            {
                                // If the mouse is over a point or cursor, show its tooltip. ::::::::::::::::

                                //DataPoint closestPoint = GetClosestPoint(newPos);
                                //GridCursor closestCursor = CursorToolTipProvider != null ? GetClosestCursor(newPos) : null;

                                //if (closestPoint != null)
                                //{
                                //    if (!closestPoint.Hide || _showHidden)
                                //    {
                                //        StringBuilder sb = new StringBuilder();
                                //        sb.Append("Y:" + closestPoint.YString + "\nX:" + closestPoint.XString);
                                //        if (ToolTipEvent != null)
                                //        {
                                //            ToolTipEventArgs args = new ToolTipEventArgs()
                                //            {
                                //                Data = closestPoint,
                                //                CursorId = -1,
                                //                Position = 0,
                                //            };
                                //            ToolTipEvent.Invoke(this, args);
                                //            sb.Append(args.Text);
                                //        }
                                //        // Display the tooltip
                                //        toolTip.Show(sb.ToString(), this, newPos.X + 15, newPos.Y);
                                //    }
                                //}
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
                                //else
                                //{
                                //    // Hide the tooltip
                                //    toolTip.Hide(this);
                                //}
                            }

                            break;
                    }




                    //// See what we're over.
                    //PolygonF hitPolygon;
                    //int hitPoint;
                    //int hitPoint2;
                    //PointF closestPoint;

                    //if (PointIsOverCorner(e.Location, out hitPolygon, out hitPoint))
                    //{
                    //    newCursor = Cursors.Arrow;
                    //}
                    //else if (PointIsOverEdge(e.Location, out hitPolygon, out hitPoint, out hitPoint2, out closestPoint))
                    //{
                    //    newCursor = _addPointCursor;
                    //}
                    //else if (PointIsOverPolygon(e.Location, out hitPolygon))
                    //{
                    //    newCursor = Cursors.Hand;
                    //}
                    //else
                    //{
                    //    if ((MouseButtons != MouseButtons.Left) && newPos != _lastMousePos) // Apparently a known issue is that showing a tooltip causes a MouseMove event to get generated.
                    //    {
                    //        // If the mouse is over a point or cursor, show its tooltip. ::::::::::::::::

                    //        //DataPoint closestPoint = GetClosestPoint(newPos);
                    //        //GridCursor closestCursor = CursorToolTipProvider != null ? GetClosestCursor(newPos) : null;

                    //        //if (closestPoint != null)
                    //        //{
                    //        //    if (!closestPoint.Hide || _showHidden)
                    //        //    {
                    //        //        StringBuilder sb = new StringBuilder();
                    //        //        sb.Append("Y:" + closestPoint.YString + "\nX:" + closestPoint.XString);
                    //        //        if (ToolTipEvent != null)
                    //        //        {
                    //        //            ToolTipEventArgs args = new ToolTipEventArgs()
                    //        //            {
                    //        //                Data = closestPoint,
                    //        //                CursorId = -1,
                    //        //                Position = 0,
                    //        //            };
                    //        //            ToolTipEvent.Invoke(this, args);
                    //        //            sb.Append(args.Text);
                    //        //        }
                    //        //        // Display the tooltip
                    //        //        toolTip.Show(sb.ToString(), this, newPos.X + 15, newPos.Y);
                    //        //    }
                    //        //}
                    //        //else if (closestCursor != null)
                    //        //{
                    //        //    // Display the tooltip
                    //        //    string s = CursorToolTipProvider(closestCursor.Id);
                    //        //    if (s.Length > 0)
                    //        //    {
                    //        //        toolTip.Show(s, this, newPos.X + 15, newPos.Y);
                    //        //    }
                    //        //    else
                    //        //    {
                    //        //        toolTip.Hide(this);
                    //        //    }
                    //        //}
                    //        //else
                    //        //{
                    //        //    // Hide the tooltip
                    //        //    toolTip.Hide(this);
                    //        //}
                    //    }
                    //}

                    _lastMousePos = newPos;

                    // Set the new cursor.
                    if (Cursor != newCursor)
                    {
                        Cursor = newCursor;
                    }
                    break;

                default:
                    // Shouldn't get others.
                    MessageBox.Show($"Grid_MouseMove() got {_drawState}");
                    break;
            }

            if(inval)
            {
                Invalidate();
            }
        }

        /// <summary>Handler for getting user scale values.</summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event Arguments</param>
        void Grid_MouseClick(object sender, MouseEventArgs e)
        {
            if (e != null && e.Button == MouseButtons.Right && Polygons.Count > 0)
            {
                ContextMenuStrip cms = new ContextMenuStrip();
                cms.ShowImageMargin = false;
                cms.Items.Add(":::::::::::::::: any???");
                //cms.Items.Add("Set Scale", null, new EventHandler(ShowChartScale));
                cms.Show(this, new Point(e.X, e.Y));
            }
        }

        /// <param name="sender">Object that triggered the event.</param>
        /// <param name="e">The Key Event arguments.</param>
        void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                ///// basic control //////
                case Keys.ControlKey:
                   // if (!_ctrlDown)
                    //{
                        _startMousePos = new Point(_lastMousePos.X, _lastMousePos.Y);
                    //}
                    //_ctrlDown = true;
                    break;

                //case Keys.ShiftKey:
                //    _shiftDown = true;
                //    break;

                /////// zoom control //////
                //case Keys.I:
                //    ZoomIn();
                //    break;

                //case Keys.O:
                //    ZoomOut();
                //    break;

                ///// zoom x or y axis only /////
                //case Keys.Y:
                //    _yDown = true;
                //    break;

                //case Keys.X:
                //    _xDown = true;
                //    break;

                ///// shift axes with ctrl-arrow //////
                //case Keys.Left:
                //    _origin.X -= _shiftSpeed;
                //    Invalidate();
                //    break;

                //case Keys.Right:
                //    _origin.X += _shiftSpeed;
                //    Invalidate();
                //    break;

                //case Keys.Up:
                //    _origin.Y -= _shiftSpeed;
                //    Invalidate();
                //    break;

                //case Keys.Down:
                //    _origin.Y += _shiftSpeed;
                //    Invalidate();
                //    break;

                ///// reset //////
                case Keys.H:
                    _firstPaint = true;
                    Invalidate();
                    break;
            }
        }

        /// <summary>?????.</summary>
        /// <param name="sender">Object that triggered the event.</param>
        /// <param name="e">The Key Event arguments.</param>
        void Grid_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                //case Keys.Y:
                //    _yDown = false;
                //    break;

                //case Keys.X:
                //    _xDown = false;
                //    break;

                case Keys.ControlKey:
                    //_ctrlDown = false;

                    //if (_dragging)
                    //{
                    //    GetSelectedPoints();
                    //    _dragging = false;
                    //    _dragCursor = null;
                    //    Invalidate();
                    //}

                    _startMousePos = new Point();
                    _endMousePos = new Point();
                    break;

                //case Keys.ShiftKey:
                //    _shiftDown = false;
                //    break;
            }
        }

        void Grid_Resize(object sender, EventArgs e)
        {
            // Force repaint of chart.
            _firstPaint = true; // Need to recalc the grid too.
            Invalidate();
        }

        /// <summary>Resets key states when control loses focus.</summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event Arguments</param>
        void Grid_LostFocus(object sender, EventArgs e)
        {
            //_ctrlDown = false;
            //_shiftDown = false;
        }

        /// <summary>Zooms in or out depending on the direction the mouse wheel is moved.</summary>
        /// <param name="sender">Object that sent triggered the event.</param>
        /// <param name="e">The particular MouseEventArgs (DoubleClick, Click, etc.).</param>
        void Grid_MouseWheel(object sender, MouseEventArgs e)
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

                //FireScaleChangeEvent();
            }
        }
        #endregion
        

        // Redraw old polygons in blue. Draw the new polygon in green. Draw the final segment dashed.
        void DrawPolygons(Graphics g)
        {
            // Draw the polygons.
            foreach (PolygonF polygon in Polygons)
            {
                // Draw the polygon. :::::::::::::::: support non closed. GraphicsPath
                g.FillPolygon(Brushes.White, polygon.Points.ToArray());
                g.DrawPolygon(Pens.Blue, polygon.Points.ToArray());

                // Draw the corners.
                foreach (PointF corner in polygon.Points)
                {
                    RectangleF rect = new RectangleF(corner.X - TARGET_SIZE, corner.Y - TARGET_SIZE, 2 * TARGET_SIZE + 1, 2 * TARGET_SIZE + 1);
                    g.FillEllipse(Brushes.White, rect);
                    g.DrawEllipse(Pens.Black, rect);
                }
            }

            // Draw the new polygon.
            if (_newPolygon != null)
            {
                // Draw the new polygon.
                if (_newPolygon.Points.Count > 1)
                {
                    g.DrawLines(Pens.Green, _newPolygon.Points.ToArray());
                }

                // Draw the newest edge.
                if (_newPolygon.Points.Count > 0)
                {
                    using (Pen dashed_pen = new Pen(Color.Green))
                    {
                        dashed_pen.DashPattern = new float[] { 3, 3 };
                        g.DrawLine(dashed_pen, _newPolygon.Points[_newPolygon.Points.Count - 1], _newPoint);
                    }
                }
            }
        }






        EditResult WhereIsPoint(PointF point)
        {
            EditResult res = new EditResult() { ploc = PointLocation.NotPertinent };
            GeometryResult gres = new GeometryResult();

            // Examine all polygons in reverse order to check the ones on top first.
            for (int i = Polygons.Count - 1; i >= 0 && res.ploc == PointLocation.NotPertinent; i--)
            {
                PolygonF polygon = Polygons[i];

                if (res.ploc == PointLocation.NotPertinent)
                {
                    // See if we're over one of the polygon's corner points.
                    gres = polygon.IsCornerPoint(point);
                    if(gres.valid)
                    {
                        res.ploc = PointLocation.OverCorner;
                        res.hitPolygon = polygon;
                        res.hitPoint = gres.pointIndex1;
                    }
                }

                if (res.ploc == PointLocation.NotPertinent)
                {
                    // Test over edge.
                    gres = polygon.IsEdgePoint(point);
                    if (gres.valid)
                    {
                        res.ploc = PointLocation.OverEdge;
                        res.hitPolygon = polygon;
                        res.hitPoint = gres.pointIndex1;
                    }
                }

                if (res.ploc == PointLocation.NotPertinent)
                {
                    // Test over area.
                    gres = polygon.ContainsPoint(point);
                    if (gres.valid)
                    {
                        res.ploc = PointLocation.OverPolygon;
                        res.hitPolygon = polygon;
                        res.hitPoint = gres.pointIndex1;
                    }
                }
            }

            return res;
        }


        #region Draw Axes and Labels
        /// <summary>Draw the axes on the chart.</summary>
        /// <param name="g">The Graphics object to use.</param>
        /// <param name="xticks">List of X Tick values.</param>
        /// <param name="yticks">List of Y Tick values.</param>
        void DrawGrid(Graphics g, out List<AxisTick> xticks, out List<AxisTick> yticks)
        {
            xticks = new List<AxisTick>();
            yticks = new List<AxisTick>();

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
                        sb.Append($"X:{incX}");
                        float tickPos = _dataRect.Left + (incX - _xMinScale) * _xZoomedScale;
                        xticks.Add(new AxisTick(sb.ToString(), tickPos));
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
                        sb.Append($"Y:{incY}");
                        float tickPos = _dataRect.Bottom - (incY - _yMinScale) * _yZoomedScale;
                        yticks.Add(new AxisTick(sb.ToString(), tickPos));
                        sb.Length = 0;

                        g.DrawLine(penGrid, _dataRect.Left + 2.0f, tickPos, Right - 23.0f, tickPos);
                    }
                }
            }
        }

        /// <summary>Overwrites non viewable region with a set of white rectangles.</summary>
        /// <param name="g">Graphics object</param>
        void ClearNonViewableRegion(Graphics g)
        {
            Brush br = Brushes.White;
            g.FillRectangle(br, 1, 1, _dataRect.Left - 1, Height);
            g.FillRectangle(br, 1, _dataRect.Bottom, Width - 1, Bottom - _dataRect.Bottom);
        }

        /// <summary>Draws tick values</summary>
        /// <param name="g">Graphics object</param>
        /// <param name="xticks">List of X Tick values</param>
        /// <param name="yticks">List of Y Tick values</param>
        void DrawTicks(Graphics g, List<AxisTick> xticks, List<AxisTick> yticks)
        {
            // Make a pen with the proper color and size for drawing the axes.
            Pen pen = new Pen(AxesColor, AxesWidth);

            // Draw axis lines.
            g.DrawLine(pen, _dataRect.Left, _dataRect.Bottom, _dataRect.Right, _dataRect.Bottom);
            g.DrawLine(pen, _dataRect.Left, _dataRect.Top, _dataRect.Left, _dataRect.Bottom);

            // Draw X-Axis Increments
            foreach (AxisTick pr in xticks)
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
            foreach (AxisTick pr in yticks)
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
        void DrawLabel(Graphics g, float transformX, float transformY, string labelText, Font font, int rotationDegrees)
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
            if (Polygons.Count > 0)
            {
                float oldXScale = _xZoomFactor;
                float oldYScale = _yZoomFactor;

                bool shouldZoom = true;

                if (!KeyboardState.IsKeyDown(Keys.Y))
                {
                    _xZoomFactor *= _zoomSpeed;

                    // Prevent shifting when zoom reaches limit
                    if (_xZoomFactor > MAX_ZOOM_LIMIT_OTHER)// && SeriesCollection[0].XType != typeof(DateTime))
                    {
                        _xZoomFactor = MAX_ZOOM_LIMIT_OTHER;
                    }
                }

                if (!KeyboardState.IsKeyDown(Keys.X) && shouldZoom)
                {
                    _yZoomFactor *= _zoomSpeed;

                    // Prevent shifting when zoom reaches limit
                    if (_yZoomFactor > MAX_ZOOM_LIMIT_OTHER)// && SeriesCollection[0].XType != typeof(DateTime))
                    {
                        _yZoomFactor = MAX_ZOOM_LIMIT_OTHER;
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
            if (Polygons.Count > 0)
            {
                float oldXScale = _xZoomFactor;
                float oldYScale = _yZoomFactor;

                bool shouldZoom = true;

                if (!KeyboardState.IsKeyDown(Keys.Y))
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
                if (!KeyboardState.IsKeyDown(Keys.X) && shouldZoom)
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
        void Recenter(float xRatio, float yRatio)
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

            //FireScaleChangeEvent();
        }
        #endregion



        
        /// <summary>Perform grid initialization.</summary>
        void InitGrid()
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
            _xActualScale = _dataRect.Width / _xRange;
            _yActualScale = _dataRect.Height / _yRange;

            // Calculate the origin X --> buffered by ([1-_xZoomFactor]/2).
            _origin.X = _xRange != 0 ?
                _dataRect.Left - _xVals.Min() * _xActualScale :
                _dataRect.Left - (_xVals.Min() * _xActualScale) + (_dataRect.Width / 2);

            // Calculate the orgin Y --> buffered by ([1-_yZoomFactor]/2).
            _origin.Y = _yRange != 0 ?
                _dataRect.Bottom + (_yVals.Min() * _yActualScale) :
                _dataRect.Bottom + (_yVals.Min() * _yActualScale) - (_dataRect.Height / 2);

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


        /// <summary>Obtain the point on the grid based on a client position.</summary>
        /// <param name="p">The PointF to restore</param>
        /// <returns>A PointF corresponding to the true coordinates (non-client) of the given PointF.</returns>
        PointF GetGridPoint(PointF p)
        {
            return new PointF((p.X - _origin.X) / _xZoomedScale, (_origin.Y - p.Y) / _yZoomedScale);
        }

        /// <summary>Obtain a client point based on a point on the chart.</summary>
        /// <param name="p">The PointF to correct</param>
        /// <returns>A PointF corresponding to the proper raw client position of the given PointF.</returns>
        PointF GetClientPoint(PointF p)
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

        /// <summary>Returns the tick increment value and sets the format specifier.</summary>
        /// <param name="tickRng">Starting tick range</param>
        /// <param name="dataRng">Data range</param>
        /// <param name="numTicks">Number of desired tick values</param>
        /// <param name="xAxis">True if x axis</param>
        /// <returns>Tick increment</returns>
        float GetTickValue(float tickRng, float dataRng, int numTicks, bool xAxis)
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




        #region Draw Cursors
        /// <summary>Draw a cursor chart using the specified graphics.</summary>
        /// <param name="g">The Graphics object to use.</param>
        /// <param name="cursor">The cursor to plot.</param>
        void DrawCursor(Graphics g, GridCursor cursor)
        {
            Pen pen = new Pen(cursor.Color, cursor.LineWidth);
            pen.DashStyle = cursor.LineType;

            // Shift the point to proper position on client screen.
            List<PointF> points = new List<PointF>();

            PointF clientPoint = GetClientPoint(new PointF(cursor.Position, _yMinScale));
            points.Add(clientPoint);
            clientPoint = GetClientPoint(new PointF(cursor.Position, _yMaxScale));
            points.Add(clientPoint);

            // Draw the lines for the corrected values.
            g.DrawLines(pen, points.ToArray());
        }

        /// <summary>Find the closest cursor to the given point.</summary>
        /// <param name="point">Mouse point</param>
        /// <returns>The closest cursor or null if not in range.</returns>
        GridCursor GetClosestCursor(PointF point)
        {
            GridCursor closest = null;

            foreach (GridCursor c in _gridCursors)
            {
                PointF clientPoint = GetClientPoint(new PointF(c.Position, point.Y));
                if (Math.Abs(point.X - clientPoint.X) < MOUSE_SELECT_RANGE)
                {
                    closest = c;
                    break;
                }
            }

            return closest;
        }
        #endregion


        #region Public methods for interacting with the cursors
        /// <summary>Add a new cursor to the collection.</summary>
        /// <param name="position"></param>
        /// <param name="color"></param>
        /// <param name="lineWidth"></param>
        /// <returns>Id of the new cursor.</returns>
        public int AddCursor(float position, Color color, float lineWidth = LINE_WIDTH)
        {
            GridCursor cursor = new GridCursor()
            {
                Position = position,
                Color = color,
                LineWidth = lineWidth
            };

            _gridCursors.Add(cursor);

            return cursor.Id;
        }

        /// <summary>Remove the specified cursor from the collection.</summary>
        /// <param name="id"></param>
        public void RemoveCursor(int id)
        {
            var qry = from c in _gridCursors where c.Id == id select c;

            if (qry.Any()) // found it
            {
                _gridCursors.Remove(qry.First());
            }
        }

        /// <summary>Remove all cursors from the collection.</summary>
        public void RemoveAllCursors()
        {
            _gridCursors.Clear();
        }

        /// <summary>Relocate the specified cursor. Client needs to call Refresh() after done.</summary>
        /// <param name="id"></param>
        /// <param name="position"></param>
        public void MoveCursor(int id, float position)
        {
            var qry = from c in _gridCursors where c.Id == id select c;

            if (qry.Any()) // found it
            {
                qry.First().Position = position;
            }
        }

        /// <summary>Find the closest cursor to the given point.</summary>
        /// <param name="point">Mouse point</param>
        /// <returns>The cursor id or -1 if not in range.</returns>
        public int CursorHit(PointF point)
        {
            int id = -1;

            GridCursor c = GetClosestCursor(point);
            if (c != null)
            {

                id = c.Id;
            }

            return id;
        }
        #endregion

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
        //}

        ///// <summary>Client wants to reduce noise.</summary>
        //public void HideTooltip()
        //{
        //    toolTip.Hide(this);
        //}
    }
}
