using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NLog;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using Nebulator.Common;

namespace Nebulator.Script
{
    public partial class Surface : Form
    {
        #region Events
        /// <summary>Reports a runtime error to listeners.</summary>
        public event EventHandler<RuntimeErrorEventArgs> RuntimeErrorEvent;

        public class RuntimeErrorEventArgs : EventArgs
        {
            public Exception Exception { get; set; } = null;
        }
        #endregion

        #region Fields
        /// <summary>My logger.</summary>
        Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>The embedded drawing control.</summary>
        SKControl _skcontrol;

        /// <summary>The current script.</summary>
        ScriptCore _script = null;

        /// <summary>Rendered bitmap for display when painting.</summary>
        System.Drawing.Bitmap _bitmap = null;

        /// <summary>For metrics.</summary>
        TimingAnalyzer _tanDraw = new TimingAnalyzer() { SampleSize = 10 };
        #endregion

        #region Lifecycle
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Surface()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            UpdateStyles();

            // Create the control.
            _skcontrol = new SKControl();
            Controls.Add(_skcontrol);

           // ResizeRedraw = true;
        }

        /// <summary>
        /// Initialize form controls.
        /// </summary>
        private void Surface_Load(object sender, EventArgs e)
        {
            // Intercept all keyboard events.
            KeyPreview = true;
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Update per new script object.
        /// </summary>
        /// <param name="script"></param>
        public void InitSurface(ScriptCore script)
        {
            if(script != null)
            {
                _script = script;
                ClientSize = new System.Drawing.Size(_script.width, _script.height);
                _script.focused = Focused;
                _script.frameCount = 0;
                _script.setup();
            }
        }

        /// <summary>
        /// Redraw if it's time and enabled.
        /// </summary>
        public void UpdateSurface()
        {
            if (_script != null && (_script._loop || _script._redraw))
            {
                // Check for resize or init.
                if(_bitmap == null || _bitmap.Width != _script.width || _bitmap.Height != _script.height)
                {
                    if (_bitmap != null)
                    {
                        _bitmap.Dispose();
                        _bitmap = null;
                    }

                    // Update client side.
                    _bitmap = new System.Drawing.Bitmap(_script.width, _script.height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                    ClientSize = new System.Drawing.Size(_script.width, _script.height);
                }

                Render();
                Invalidate();
            }
        }
        #endregion

        #region Painting
        /// <summary>
        /// Calls the script code that generates the bmp to paint later.
        /// </summary>
        void Render()
        {
            var w = _script.width; // Width;
            var h = _script.height; // Height;

            var data = _bitmap.LockBits(new System.Drawing.Rectangle(0, 0, w, h), System.Drawing.Imaging.ImageLockMode.WriteOnly, _bitmap.PixelFormat);

            using (var skSurface = SKSurface.Create(w, h, SKImageInfo.PlatformColorType, SKAlphaType.Premul, data.Scan0, w * 4))
            {
                // Note: Need exception handling here to protect from user script errors.
                try
                {
                    // Hand over to the script for drawing on.
                    _script._canvas = skSurface.Canvas;

                    // Some housekeeping.
                    _script.pMouseX = _script.mouseX;
                    _script.pMouseY = _script.mouseY;
                    _script._redraw = false;

                    //_tanDraw.Arm();

                    // Execute the user script code.
                    _script.frameCount++;
                    _script.draw();

                    //if (_tanDraw.Grab())
                    //{
                    //    _logger.Info("UI tan: " + _tanDraw.ToString());
                    //    _logger.Info(string.Join(" ", _tanDraw.Times.ConvertAll(t => t.ToString("0.00"))));
                    //}
                }
                catch (Exception ex)
                {
                    RuntimeErrorEvent?.Invoke(this, new RuntimeErrorEventArgs() { Exception = ex });
                }
            }

            _bitmap.UnlockBits(data);
        }

        /// <summary>
        /// Renders the stored bitmap to the UI.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (_bitmap != null)
            {
                e.Graphics.DrawImage(_bitmap, new System.Drawing.Point(0, 0));
            }
        }
        #endregion

        #region Mouse handling
        /// <summary>
        /// Event handler.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if(_script != null)
            {
                ProcessMouseEvent(e);
                _script.mouseIsPressed = true;
                _script.mousePressed();
            }
            base.OnMouseDown(e);
        }

        /// <summary>
        /// Event handler.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_script != null)
            {
                ProcessMouseEvent(e);
                _script.mouseIsPressed = false;
                _script.mouseReleased();
            }
            base.OnMouseUp(e);
        }

        /// <summary>
        /// Event handler.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_script != null)
            {
                ProcessMouseEvent(e);
                if (_script.mouseIsPressed)
                {
                    _script.mouseDragged();
                }
                else
                {
                    _script.mouseMoved();
                }
            }
            base.OnMouseMove(e);
        }

        /// <summary>
        /// Event handler.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (_script != null)
            {
                ProcessMouseEvent(e);
                _script.mouseClicked();
            }
            base.OnMouseClick(e);
        }

        /// <summary>
        /// Event handler.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (_script != null)
            {
                ProcessMouseEvent(e);
                _script.mouseWheel();
            }
            base.OnMouseWheel(e);
        }

        /// <summary>
        /// Event handler.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseEnter(EventArgs e)
        {
            if (_script != null)
            {
                _script.focused = Focused;
            }
        }

        /// <summary>
        /// Event handler.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            if (_script != null)
            {
                _script.focused = Focused;
            }
        }

        /// <summary>
        /// Common routine to update mouse stuff.
        /// </summary>
        /// <param name="e"></param>
        void ProcessMouseEvent(MouseEventArgs e)
        {
            if (_script != null)
            {
                _script.mouseX = e.X;
                _script.mouseY = e.Y;
                _script.mouseWheelValue = e.Delta;

                switch (e.Button)
                {
                    case MouseButtons.Left: _script.mouseButton = ScriptCore.LEFT; break;
                    case MouseButtons.Right: _script.mouseButton = ScriptCore.RIGHT; break;
                    case MouseButtons.Middle: _script.mouseButton = ScriptCore.CENTER; break;
                    default: _script.mouseButton = 0; break;
                }
            }
        }
        #endregion

        #region Keyboard handling
        /// <summary>
        /// Event handler for keys.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_script != null)
            {
                _script.keyIsPressed = false;

                // Decode character, maybe.
                var v = Utils.KeyToChar(e.KeyCode, e.Modifiers);
                ProcessKeys(v);

                if (_script.key != 0)
                {
                    // Valid character.
                    _script.keyIsPressed = true;
                    // Notify client.
                    _script.keyPressed();
                }
            }
        }

        /// <summary>
        /// Event handler for keys.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (_script != null)
            {
                _script.keyIsPressed = false;

                // Decode character, maybe.
                var v = Utils.KeyToChar(e.KeyCode, e.Modifiers);
                ProcessKeys(v);

                if (_script.key != 0)
                {
                    // Valid character.
                    _script.keyIsPressed = false;
                    // Notify client.
                    _script.keyReleased();
                    // Now reset keys.
                    _script.key = (char)0;
                    _script.keyCode = 0;
                }
            }
        }

        /// <summary>
        /// Event handler for keys.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (_script != null)
            {
                _script.key = e.KeyChar;
                _script.keyTyped();
            }
        }

        /// <summary>
        /// Convert generic utility output to flavor that Processing understands.
        /// </summary>
        /// <param name="keys"></param>
        void ProcessKeys((char ch, List<Keys> keyCodes) keys)
        {
            _script.keyCode = 0;
            _script.key = keys.ch;

            // Check modifiers.
            if (keys.keyCodes.Contains(Keys.Control))
            {
                _script.keyCode |= ScriptCore.CTRL;
            }

            if (keys.keyCodes.Contains(Keys.Alt))
            {
                _script.keyCode |= ScriptCore.ALT;
            }

            if (keys.keyCodes.Contains(Keys.Shift))
            {
                _script.keyCode |= ScriptCore.SHIFT;
            }

            if (keys.keyCodes.Contains(Keys.Left))
            {
                _script.keyCode |= ScriptCore.LEFT;
                _script.key = (char)ScriptCore.CODED;
            }

            if (keys.keyCodes.Contains(Keys.Right))
            {
                _script.keyCode |= ScriptCore.RIGHT;
                _script.key = (char)ScriptCore.CODED;
            }

            if (keys.keyCodes.Contains(Keys.Up))
            {
                _script.keyCode |= ScriptCore.UP;
                _script.key = (char)ScriptCore.CODED;
            }

            if (keys.keyCodes.Contains(Keys.Down))
            {
                _script.keyCode |= ScriptCore.DOWN;
                _script.key = (char)ScriptCore.CODED;
            }
        }
        #endregion
    }
}
