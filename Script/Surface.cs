using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Nebulator.Common;


namespace Nebulator.Script
{
    /// <summary>
    /// The client hosts this control in their UI. It performs the actual graphics drawing and input.
    /// </summary>
    public partial class Surface : UserControl
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
        /// <summary>The current script.</summary>
        ScriptCore _script = null;
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

            ResizeRedraw = true;
        }

        /// <summary>
        /// Update per new script object.
        /// </summary>
        /// <param name="script"></param>
        public void InitScript(ScriptCore script)
        {
            _script = script;
            _script.width = Width;
            _script.height = Height;
            _script.focused = Focused;
        }

        /// <summary>
        /// Redraw if it's time and enabled.
        /// </summary>
        public void UpdateSurface()
        {
            if (_script != null && (_script._loop || _script._redraw))
            {
                Invalidate();
            }
        }
        #endregion

        #region Painting
        /// <summary>
        /// Calls the script code that generates the bmp to draw.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (_script != null && (_script._loop || _script._redraw))
            {
                try
                {
                    _script._redraw = false;

        //            e.Graphics.Clear(_script._bgColor); // TODO might need a property to indicate overdraw current or draw new.

                    e.Graphics.CompositingMode = CompositingMode.SourceOver;
                    e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
                    e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    e.Graphics.SmoothingMode = _script._smooth ? SmoothingMode.HighQuality : SmoothingMode.HighSpeed;

                    // Hand over to the script for drawing.
                    _script._gr = e.Graphics;

                    // Some housekeeping.
                    _script.pMouseX = _script.mouseX;
                    _script.pMouseY = _script.mouseY;

                    // Execute the user script code.
                    _script.frameCount++;
                    _script.draw();
                }
                catch (Exception ex)
                {
                    RuntimeErrorEvent?.Invoke(this, new RuntimeErrorEventArgs() { Exception = ex });
                }
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
                _script.mousePressedP = true;
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
                _script.mousePressedP = false;
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
                if (_script.mousePressedP)
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
            base.OnMouseClick(e); /// ???????
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
            base.OnMouseWheel(e); /// ???????
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
                _script.keyPressedP = false;

                // Decode character, maybe.
                var v = Utils.KeyToChar(e.KeyCode, e.Modifiers);
                ProcessKeys(v);

                if (_script.key != 0)
                {
                    // Valid character.
                    _script.keyPressedP = true;
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
                _script.keyPressedP = false;

                // Decode character, maybe.
                var v = Utils.KeyToChar(e.KeyCode, e.Modifiers);
                ProcessKeys(v);

                if (_script.key != 0)
                {
                    // Valid character.
                    _script.keyPressedP = false;
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

        #region Window status updates
        private void Surface_Resize(object sender, EventArgs e)
        {
            if (_script != null)
            {
                _script.width = Width;
                _script.height = Height;
            }
        }

        private void Surface_Enter(object sender, EventArgs e)
        {
            if (_script != null)
            {
                _script.focused = Focused;
            }
        }

        private void Surface_Leave(object sender, EventArgs e)
        {
            if (_script != null)
            {
                _script.focused = Focused;
            }
        }
        #endregion
    }
}
