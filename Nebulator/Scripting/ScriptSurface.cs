using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Windows.Forms;
using Nebulator.Common;
using Nebulator.FastTimer;

// TODO2 Make the graphics faster and smoother.
// I messed with SharpDx and OpenTk but it's a jump in complexity that I don't feel like doing right now.
// Considered WPF but the boost may not be all that big for these simple 2D graphics.
// But did change to GDI+ buffered per: http://kynosarges.org/WpfPerformance.html.


namespace Nebulator.Scripting
{
    /// <summary>
    /// Processing emulation script stuff.
    /// </summary>
    public partial class Script
    {
        #region SurfaceControl
        /// <summary>
        /// Dynamically created user control that the client can site in a form.
        /// All event handlers are supported in the Script class for close binding with properties and rendering.
        /// </summary>
        class SurfaceControl : UserControl
        {
            public SurfaceControl()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
                UpdateStyles();
                BackColor = Globals.UserSettings.BackColor;
            }
        }
        #endregion

        #region Public access
        /// <summary>
        /// Create an instance of the user control. Performs event binding.
        /// </summary>
        public UserControl CreateSurface()
        {
            _surface?.Dispose();

            _surface = new SurfaceControl();

            // Our event handlers.
            _surface.Paint += Surface_Paint;
            _surface.MouseDown += Surface_MouseDown;
            _surface.MouseUp += Surface_MouseUp;
            _surface.MouseClick += Surface_MouseClick;
            _surface.MouseDoubleClick += Surface_MouseDoubleClick;
            _surface.MouseMove += Surface_MouseMove;
            _surface.MouseWheel += Surface_MouseWheel;
            _surface.KeyDown += Surface_KeyDown;
            _surface.KeyUp += Surface_KeyUp;
            _surface.KeyPress += Surface_KeyPress;

            return _surface;
        }

        /// <summary>
        /// Redraw if time and enabled.
        /// </summary>
        public void UpdateSurface()
        {
            if (_loop || _redraw)
            {
                _surface.Invalidate();
                _redraw = false;
            }
        }
        #endregion

        #region Painting
        /// <summary>
        /// Calls the script code that generates the bmp to draw.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Surface_Paint(object sender, PaintEventArgs e)
        {
            //??? _surface.OnPaint(args);

            BufferedGraphicsContext context = new BufferedGraphicsContext();
            context.MaximumBuffer = _surface.ClientSize;

            using (BufferedGraphics buffer = context.Allocate(e.Graphics, _surface.ClientRectangle))
            {
                buffer.Graphics.Clear(_bgColor);

                buffer.Graphics.CompositingMode = CompositingMode.SourceOver;
                buffer.Graphics.CompositingQuality = CompositingQuality.HighQuality;
                buffer.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                buffer.Graphics.SmoothingMode = _smooth ? SmoothingMode.HighQuality : SmoothingMode.HighSpeed;

                _gr = buffer.Graphics;

                // Some housekeeping.
                pMouseX = mouseX;
                pMouseY = mouseY;

                // Measure and alert if too slow, or throttle.
                _tanUi.Arm();

                // Execute the user code.
                draw();

                buffer.Render();
            }

            context.Dispose();
        }
        #endregion

        #region Mouse handling
        /// <summary>
        /// Event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Surface_MouseDown(object sender, MouseEventArgs e)
        {
            ProcessMouseEvent(e);
            mousePressedP = true;
            mousePressed();
        }

        /// <summary>
        /// Event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Surface_MouseUp(object sender, MouseEventArgs e)
        {
            ProcessMouseEvent(e);
            mousePressedP = false;
            mouseReleased();
        }

        /// <summary>
        /// Event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Surface_MouseMove(object sender, MouseEventArgs e)
        {
            ProcessMouseEvent(e);
            if (mousePressedP)
            {
                mouseDragged();
            }
            else
            {
                mouseMoved();
            }
        }

        /// <summary>
        /// Event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Surface_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // Not supported in processing
        }

        /// <summary>
        /// Event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Surface_MouseClick(object sender, MouseEventArgs e)
        {
            ProcessMouseEvent(e);
            mouseClicked();
        }

        /// <summary>
        /// Event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Surface_MouseWheel(object sender, MouseEventArgs e)
        {
            ProcessMouseEvent(e);
            mouseWheel();
        }

        /// <summary>
        /// Common routine to update mouse stuff.
        /// </summary>
        /// <param name="e"></param>
        void ProcessMouseEvent(MouseEventArgs e)
        {
            mouseX = e.X;
            mouseY = e.Y;
            mouseWheelValue = e.Delta;

            switch (e.Button)
            {
                case MouseButtons.Left: mouseButton = LEFT; break;
                case MouseButtons.Right: mouseButton = RIGHT; break;
                case MouseButtons.Middle: mouseButton = CENTER; break;
                default: mouseButton = 0; break;
            }
        }
        #endregion

        #region Keyboard handling - see MainForm region "Keyboard handling"
        /// <summary>
        /// Event handler for keys.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Surface_KeyDown(object sender, KeyEventArgs e)
        {
            keyPressedP = false;

            // Decode character, maybe.
            var v = Utils.KeyToChar(e.KeyCode, e.Modifiers);
            ProcessKeys(v);

            if (key != 0)
            {
                // Valid character.
                keyPressedP = true;
                // Notify client.
                keyPressed();
            }
        }

        /// <summary>
        /// Event handler for keys.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Surface_KeyUp(object sender, KeyEventArgs e)
        {
            keyPressedP = false;

            // Decode character, maybe.
            var v = Utils.KeyToChar(e.KeyCode, e.Modifiers);
            ProcessKeys(v);

            if (key != 0)
            {
                // Valid character.
                keyPressedP = false;
                // Notify client.
                keyReleased();
                // Now reset keys.
                key = (char)0;
                keyCode = 0;
            }
        }

        /// <summary>
        /// Event handler for keys.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Surface_KeyPress(object sender, KeyPressEventArgs e)
        {
            key = e.KeyChar;
            keyTyped();
        }

        /// <summary>
        /// Convert generic utility output to flavor that Processing understands.
        /// </summary>
        /// <param name="keys"></param>
        void ProcessKeys((char ch, List<Keys> keyCodes) keys)
        {
            keyCode = 0;
            key = keys.ch;

            // Check modifiers.
            if (keys.keyCodes.Contains(Keys.Control))
            {
                keyCode |= CTRL;
            }

            if (keys.keyCodes.Contains(Keys.Alt))
            {
                keyCode |= ALT;
            }

            if (keys.keyCodes.Contains(Keys.Shift))
            {
                keyCode |= SHIFT;
            }

            if (keys.keyCodes.Contains(Keys.Left))
            {
                keyCode |= LEFT;
                key = (char)CODED;
            }

            if (keys.keyCodes.Contains(Keys.Right))
            {
                keyCode |= RIGHT;
                key = (char)CODED;
            }

            if (keys.keyCodes.Contains(Keys.Up))
            {
                keyCode |= UP;
                key = (char)CODED;
            }

            if (keys.keyCodes.Contains(Keys.Down))
            {
                keyCode |= DOWN;
                key = (char)CODED;
            }
        }
        #endregion
    }
}
