using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Windows.Forms;
using NLog;
using Nebulator.Common;


namespace Nebulator.Scripting
{
    /// <summary>
    /// Processing emulation script stuff.
    /// </summary>
    public partial class Script
    {
        #region SurfaceControl stuff
        /// <summary>
        /// Dynamically created shell control that the client can site in a form.
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

        /// <summary>
        /// Create an instance of the user control. Performs event binding.
        /// </summary>
        void CreateSurface()
        {
            Surface?.Dispose();

            Surface = new SurfaceControl();

            // Our event handlers.
            Surface.Paint += Surface_Paint;
            Surface.MouseDown += Surface_MouseDown;
            Surface.MouseUp += Surface_MouseUp;
            Surface.MouseClick += Surface_MouseClick;
            Surface.MouseDoubleClick += Surface_MouseDoubleClick;
            Surface.MouseMove += Surface_MouseMove;
            Surface.MouseWheel += Surface_MouseWheel;
            Surface.KeyDown += Surface_KeyDown;
            Surface.KeyUp += Surface_KeyUp;
        }

        /// <summary>
        /// Update the SurfaceControl contents. Client should catch exceptions to handle user script errors.
        /// </summary>
        public void Render()
        {
            _bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            _gr = Graphics.FromImage(_bmp);
            _gr.SmoothingMode = _smooth ? SmoothingMode.AntiAlias : SmoothingMode.None;
            _gr.Clear(_bgColor);

            // Some housekeeping.
            pMouseX = mouseX;
            pMouseY = mouseY;

            // Execute the user code.
            draw();

            // Force a redraw.
            Surface.Refresh();
        }

        /// <summary>
        /// Calls the script code that generates the bmp to draw.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Surface_Paint(object sender, PaintEventArgs e)
        {
            // Render to the surface.
            if(!(_bmp is null))
            {
                e.Graphics.DrawImageUnscaled(_bmp, 0, 0);
            }
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
            ProcessMouse(e);
            mousePressed = true;
            mousePressedEvt();
        }

        /// <summary>
        /// Event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Surface_MouseUp(object sender, MouseEventArgs e)
        {
            ProcessMouse(e);
            mousePressed = false;
            mouseReleased();
        }

        /// <summary>
        /// Event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Surface_MouseMove(object sender, MouseEventArgs e)
        {
            ProcessMouse(e);
            if (mousePressed)
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
            ProcessMouse(e);
            mouseClicked();
        }

        /// <summary>
        /// Event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Surface_MouseWheel(object sender, MouseEventArgs e)
        {
            ProcessMouse(e);
            mouseWheel();
        }

        /// <summary>
        /// Common routine to update mouse stuff.
        /// </summary>
        /// <param name="e"></param>
        void ProcessMouse(MouseEventArgs e)
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

        #region Keyboard handling
        /// <summary>
        /// Event handler for keys.
        /// How windows handles key presses, i.e Shift+A, you'll get:
        /// - KeyDown: KeyCode=Keys.ShiftKey, KeyData=Keys.ShiftKey | Keys.Shift, Modifiers=Keys.Shift
        /// - KeyDown: KeyCode=Keys.A, KeyData=Keys.A | Keys.Shift, Modifiers=Keys.Shift
        /// - KeyPress: KeyChar='A'
        /// - KeyUp: KeyCode=Keys.A
        /// - KeyUp: KeyCode=Keys.ShiftKey
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Surface_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Alt:
                    alt = true;
                    break;

                case Keys.Control:
                    ctrl = true;
                    break;

                case Keys.Shift:
                    shift = true;
                    break;

                default:
                    char? c = Decode(e.KeyCode, (e.Modifiers & Keys.Shift) != 0);
                    if (c != null)
                    {
                        key = (char)c;
                        keyPressed();
                    }
                    break;
            }
        }

        /// <summary>
        /// Event handler for keys.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Surface_KeyUp(object sender, KeyEventArgs e)
        {
            char? c = Decode(e.KeyCode);
            if (c != null)
            {
                // Notify first.
                key = (char)c;
                keyReleased();
            }

            // Now reset all.
            key = ' ';
            alt = false;
            ctrl = false;
            shift = false;
        }

        /// <summary>
        /// General purpose decoder for keys.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="shift"></param>
        /// <returns></returns>
        char? Decode(Keys key, bool shift = false)
        {
            char? c = null; // means not a useful char
            keyCode = 0; // default

            switch (key)
            {
                case Keys.Back:
                    c = (char)BACKSPACE;
                    break;

                case Keys.Tab:
                    c = (char)TAB;
                    break;

                case Keys.Enter:
                    c = (char)ENTER;
                    break;

                case Keys.Escape:
                    c = (char)ESC;
                    break;

                case Keys.Delete:
                    c = (char)DELETE;
                    break;

                case Keys.Space:
                    c = ' ';
                    break;

                case Keys.Up:
                    c = (char)CODED;
                    keyCode = UP;
                    break;

                case Keys.Down:
                    c = (char)CODED;
                    keyCode = DOWN;
                    break;

                case Keys.Left:
                    c = (char)CODED;
                    keyCode = LEFT;
                    break;

                case Keys.Right:
                    c = (char)CODED;
                    keyCode = RIGHT;
                    break;

                default:
                    if (key >= Keys.A && key <= Keys.Z)
                    {
                        c = (char)(shift ? (int)key : (int)key + 32);
                    }
                    else
                    {
                        c = null;
                        keyCode = 0;
                    }
                    break;
            }

            return c;
        }
        #endregion
    }
}
