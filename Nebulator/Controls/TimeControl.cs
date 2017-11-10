using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Nebulator.Common;


namespace Nebulator.Controls
{
    public partial class TimeControl : UserControl
    {
        #region Fields
        int _major = 0;
        int _maxMajor = 100;
        int _minor = 0;
        int _lastPos = 0;
        // Main font.
        Font _font1 = new Font("Consolas", 24, FontStyle.Regular, GraphicsUnit.Point, 0);
        // Secondary font.
        Font _font2 = new Font("Consolas", 18, FontStyle.Regular, GraphicsUnit.Point, 0);
        #endregion

        #region Properties
        /// <summary>
        /// Current major value. Limited to three digits.
        /// </summary>
        public int Major
        {
            get { return _major; }
            set { _major = value % 999; Invalidate(); }
        }

        /// <summary>
        /// Current minor value. Limited to two digits.
        /// </summary>
        public int Minor
        {
            get { return _minor; }
            set { _minor = value % 99; Invalidate(); }
        }

        /// <summary>
        /// Largest major value.
        /// </summary>
        public int MaxMajor
        {
            get { return _maxMajor; }
            set { _maxMajor = value % 999; Invalidate(); }
        }

        /// <summary>
        /// For styling.
        /// </summary>
        public Color ControlColor { get; set; } = Color.Orange;

        /// <summary>
        /// All the important time points with their names.
        /// </summary>
        [ReadOnly(true)]
        [Browsable(false)]
        public Dictionary<Time, string> TimeDefs { get; set; } = new Dictionary<Time, string>();
        #endregion

        /// <summary>
        /// Value changed event.
        /// </summary>
        public event EventHandler ValueChanged;

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

        /// <summary>
        /// Draw the slider.
        /// </summary>
        protected override void OnPaint(PaintEventArgs pe)
        {
            // Outline.
            pe.Graphics.DrawRectangle(Pens.Black, 0, 0, Width - 1, Height - 1);

            // Internal.
            Brush brush = new SolidBrush(ControlColor);
            if(MaxMajor != 0)
            {
                pe.Graphics.FillRectangle(brush, 1, 1, ((Width - 2) * Major / MaxMajor), Height - 2);
            }

            // Text.
            StringFormat format = new StringFormat()
            {
                LineAlignment = StringAlignment.Center,
                Alignment = StringAlignment.Near
            };

            string sval = $"{Major:000}.{Minor:00}";
            pe.Graphics.DrawString(sval, _font1, Brushes.Black, ClientRectangle, format);

            Rectangle r2 = new Rectangle(ClientRectangle.X + 120, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);
            sval = GetTimeDef(_major);
            pe.Graphics.DrawString(sval, _font2, Brushes.Black, r2, format);

            //base.OnPaint(pe);
        }

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
        /// Common updater.
        /// </summary>
        /// <param name="x"></param>
        private void SetValueFromMouse(int x)
        {
            _minor = 0;
            Major = GetValueFromMouse(x);
            ValueChanged?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Convert to internal units.
        /// </summary>
        /// <param name="x"></param>
        private int GetValueFromMouse(int x)
        {
            int val = Utils.Constrain(x * MaxMajor / Width, 0, MaxMajor);
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
                    Major++;
                    e.IsInputKey = true;
                    break;

                case Keys.Subtract:
                case Keys.Down:
                    Major--;
                    e.IsInputKey = true;
                    break;
            }
        }
    }
}
