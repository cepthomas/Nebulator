using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Nebulator.Common;
using Nebulator.Protocol;


namespace Nebulator.Controls
{
    public partial class TimeControl : UserControl
    {
        #region Fields
        Time _current = new Time();
        int _maxTick = 0;
        int _lastPos = 0;
        // Main font.
        Font _font1 = new Font("Consolas", 24, FontStyle.Regular, GraphicsUnit.Point, 0);
        // Secondary font.
        Font _font2 = new Font("Consolas", 18, FontStyle.Regular, GraphicsUnit.Point, 0);
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
                if (_current.Tock == 0)
                {
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// Largest Tick value.
        /// </summary>
        public int MaxTick
        {
            get { return _maxTick; }
            set { _maxTick = value % 999; Invalidate(); }
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
        /// All the important time points with their names. Used also by tooltip.
        /// </summary>
        [ReadOnly(true)]
        [Browsable(false)]
        public Dictionary<Time, string> TimeDefs { get; set; } = new Dictionary<Time, string>();
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
            // Outline.
            pe.Graphics.DrawRectangle(Pens.Black, 0, 0, Width - 1, Height - 1);

            // Internal.
            Brush brush = new SolidBrush(ControlColor);
            if(ShowProgress && MaxTick != 0 && _current.Tick < _maxTick)
            {
                pe.Graphics.FillRectangle(brush, 1, 1, ((Width - 2) * _current.Tick / _maxTick), Height - 2);
            }

            // Text.
            StringFormat format = new StringFormat()
            {
                LineAlignment = StringAlignment.Center,
                Alignment = StringAlignment.Near
            };

#if _SHOW_TOCK
            // Also need to make control 220px wide.
            string sval = $"{Major:000}.{Minor:00}";
            pe.Graphics.DrawString(sval, _font1, Brushes.Black, ClientRectangle, format);
            Rectangle r2 = new Rectangle(ClientRectangle.X + 120, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);
            sval = GetTimeDef(_current.Tick);
            pe.Graphics.DrawString(sval, _font2, Brushes.Black, r2, format);
#else
            string sval = $"{_current.Tick:000}";
            pe.Graphics.DrawString(sval, _font1, Brushes.Black, ClientRectangle, format);

            Rectangle r2 = new Rectangle(ClientRectangle.X + 66, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);
            sval = GetTimeDef(_current.Tick);
            pe.Graphics.DrawString(sval, _font2, Brushes.Black, r2, format);
#endif
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
                    _current.Tick++;
                    e.IsInputKey = true;
                    break;

                case Keys.Subtract:
                case Keys.Down:
                    _current.Tick--;
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
            _current.Tick = GetValueFromMouse(x);
            _current.Tock = 0;
            ValueChanged?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Convert to internal units.
        /// </summary>
        /// <param name="x"></param>
        private int GetValueFromMouse(int x)
        {
            int val = Utils.Constrain(x * MaxTick / Width, 0, MaxTick);
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

            foreach(KeyValuePair<Time, string> kv in TimeDefs)
            {
                if(kv.Key.Tick > val)
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
