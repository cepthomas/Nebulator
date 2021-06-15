using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using NBagOfTricks;
using Nebulator.Common;


namespace Nebulator
{
    public partial class TimeControl : UserControl
    {
        #region Fields
        Time _current = new Time();
        int _maxBeat = 0;
        int _lastPos = 0;
        Font _fontLarge = new Font("Consolas", 24, FontStyle.Regular, GraphicsUnit.Point, 0);
        Font _fontSmall = new Font("Consolas", 14, FontStyle.Regular, GraphicsUnit.Point, 0);
        #endregion

        #region Properties
        /// <summary>
        /// Current value. Copy in and out to avoid holding a reference to the global time.
        /// </summary>
        public Time CurrentTime
        {
            get
            {
                return new Time(_current);
            }
            set
            {
                _current = new Time(value);
                if (_current.Tick == 0)
                {
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// Largest beat value.
        /// </summary>
        public int MaxBeat
        {
            get { return _maxBeat; }
            set { _maxBeat = value % 999; Invalidate(); }
        }

        /// <summary>
        /// For styling.
        /// </summary>
        public Color ControlColor { get; set; } = Color.Orange;

        /// <summary>
        /// Show/hide progress.
        /// </summary>
        public bool ShowProgress { get; set; } = true;

        /// <summary>
        /// All the important beat points with their names. Used also by tooltip.
        /// </summary>
        [ReadOnly(true)]
        [Browsable(false)]
        public Dictionary<int, string> TimeDefs { get; set; } = new Dictionary<int, string>();
        #endregion

        #region Events
        /// <summary>
        /// Value changed event.
        /// </summary>
        public event EventHandler ValueChanged;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public TimeControl()
        {
            InitializeComponent();
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }

        /// <summary>
        /// Initialize everything.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimeControl_Load(object sender, EventArgs e)
        {
            toolTip.SetToolTip(this, "Current time");
            Invalidate();
        }
        #endregion

        #region Drawing
        /// <summary>
        /// Draw the slider.
        /// </summary>
        protected override void OnPaint(PaintEventArgs pe)
        {
            // Setup.
            pe.Graphics.Clear(UserSettings.TheSettings.BackColor);
            Brush brush = new SolidBrush(ControlColor);
            Pen pen = new Pen(ControlColor);

            // Draw border.
            Pen penBorder = new Pen(Color.Black, Utils.BORDER_WIDTH);
            pe.Graphics.DrawRectangle(penBorder,
                0,
                0,
                Width - 1, 
                Height - 1);

            // Draw data.
            Rectangle drawArea = Rectangle.Inflate(ClientRectangle, -Utils.BORDER_WIDTH, -Utils.BORDER_WIDTH);

            if(ShowProgress && MaxBeat != 0 && _current.Beat < _maxBeat)
            {
                pe.Graphics.FillRectangle(brush,
                    Utils.BORDER_WIDTH,
                    Utils.BORDER_WIDTH,
                    (Width - 2 * Utils.BORDER_WIDTH) * _current.Beat / _maxBeat,
                    Height - 2 * Utils.BORDER_WIDTH);
            }

            // Text.
            StringFormat format = new StringFormat()
            {
                LineAlignment = StringAlignment.Center,
                Alignment = StringAlignment.Near
            };

            string sval = $"{_current.Beat:000}";
            pe.Graphics.DrawString(sval, _fontLarge, Brushes.Black, ClientRectangle, format);

            Rectangle r2 = new Rectangle(ClientRectangle.X + 66, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);
            sval = GetTimeDef(_current.Beat);
            pe.Graphics.DrawString(sval, _fontSmall, Brushes.Black, r2, format);
        }
        #endregion

        #region UI handlers
        /// <summary>
        /// Handle mouse position including dragging.
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                SetValueFromMouse(e.X);
            }
            else
            {
                int newPos = GetValueFromMouse(e.X);

                if (newPos != _lastPos)
                {
                    string s = GetTimeDef(newPos);
                    toolTip.SetToolTip(this, $"{newPos}.00 {s}");
                    _lastPos = newPos;
                }
            }
            base.OnMouseMove(e);
        }

        /// <summary>
        /// Handle dragging.
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            SetValueFromMouse(e.X);
            base.OnMouseDown(e);
        }

        /// <summary>
        /// Fine adjustment.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimeControl_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            switch (e.KeyData)
            {
                case Keys.Add:
                case Keys.Up:
                    _current.Beat++;
                    e.IsInputKey = true;
                    break;

                case Keys.Subtract:
                case Keys.Down:
                    _current.Beat--;
                    e.IsInputKey = true;
                    break;
            }
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Common updater.
        /// </summary>
        /// <param name="x"></param>
        private void SetValueFromMouse(int x)
        {
            _current.Beat = GetValueFromMouse(x);
            _current.Tick = 0;
            ValueChanged?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Convert to internal units.
        /// </summary>
        /// <param name="x"></param>
        private int GetValueFromMouse(int x)
        {
            int val = MathUtils.Constrain(x * MaxBeat / Width, 0, MaxBeat);
            return val;
        }

        /// <summary>
        /// Gets the time def string associated with val.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private string GetTimeDef(int val)
        {
            string s = "";

            foreach(KeyValuePair<int, string> kv in TimeDefs)
            {
                if(kv.Key > val)
                {
                    break;
                }
                else
                {
                    s = kv.Value;
                }
            }

            return s;
        }
        #endregion
    }
}
