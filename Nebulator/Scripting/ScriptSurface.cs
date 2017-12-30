using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Windows.Forms;
using Nebulator.Common;
using Nebulator.FastTimer;

// TODO1 graphics faster: - try drawRecursive() in script. SharpDx? Separate thread? https://stackoverflow.com/questions/26220964/sharpdxhow-to-place-sharpdx-window-in-winforms-window

namespace Nebulator.Scripting
{
    /// <summary>
    /// Processing emulation script stuff.
    /// </summary>
    public partial class Script
    {
        #region SurfaceControl stuff
        /// <summary>
        /// Dynamically created user control that the client can site in a form.
        /// All event handlers are supported in the Script class for close binding with properties and rendering.
        /// </summary>
        class SurfaceControl : UserControl // TODO1 clean up the script/surface interface?
        {
            public SurfaceControl()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
                UpdateStyles();
                BackColor = Globals.UserSettings.BackColor;
            }
        }

        /// <summary>
        /// Redraw if time and enabled.
        /// </summary>
        public void UpdateSurface()
        {
            if(_loop || _redraw)
            {
                Surface.Invalidate();
                _redraw = false;
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
            Surface.KeyPress += Surface_KeyPress;
        }

        ///// <summary>
        ///// Calls the script code that generates the bmp to draw.
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //void Surface_Paint(object sender, PaintEventArgs e)
        //{
        //    _gr = e.Graphics;
        //    //_gr.SmoothingMode = _smooth ? SmoothingMode.AntiAlias : SmoothingMode.None;
        //    _gr.SmoothingMode = _smooth ? SmoothingMode.HighQuality : SmoothingMode.HighSpeed;
        //    _gr.Clear(_bgColor);

        //    // Some housekeeping.
        //    pMouseX = mouseX;
        //    pMouseY = mouseY;

        //    // Measure and alert if too slow, or throttle.
        //    _tanUi.Arm();

        //    // Execute the user code.
        //    draw();

        //    //println($"draw() took {_tanUi.ReadOne()}");
        //}


        void Surface_Paint(object sender, PaintEventArgs args) // TODO1 smoother?
        {
            // From http://kynosarges.org/WpfPerformance.html
            // GDI+ buffered.

 //???           Surface.OnPaint(args);
            Point[] points = new Point[3];

            BufferedGraphicsContext context = new BufferedGraphicsContext();
            context.MaximumBuffer = Surface.ClientSize;

            using (BufferedGraphics buffer = context.Allocate(args.Graphics, Surface.ClientRectangle))
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


        //protected override void OnPaint(PaintEventArgs args)
        //{
        //    base.OnPaint(args);
        //    Point[] points = new Point[3];

        //    BufferedGraphicsContext context = new BufferedGraphicsContext();
        //    context.MaximumBuffer = ClientSize;

        //    using (BufferedGraphics buffer = context.Allocate(args.Graphics, ClientRectangle))
        //    {
        //        buffer.Graphics.Clear(BackColor);

        //        buffer.Graphics.CompositingMode = CompositingMode.SourceOver;
        //        buffer.Graphics.CompositingQuality = CompositingQuality.HighQuality;
        //        buffer.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        //        buffer.Graphics.SmoothingMode = (App.AntiAliased ? SmoothingMode.HighQuality : SmoothingMode.HighSpeed);

        //        for (int i = 0; i < App.Iterations; i++)
        //        {
        //            GdiObjects.GetVertices(i, points);

        //            if (App.Mode != TestMode.PensOnly)
        //            {
        //                Brush brush = GdiObjects.GetBrush(i);
        //                buffer.Graphics.FillPolygon(brush, points);
        //            }

        //            if (App.Mode != TestMode.BrushesOnly)
        //            {
        //                Pen pen = GdiObjects.GetPen(i);
        //                buffer.Graphics.DrawPolygon(pen, points);
        //            }
        //        }

        //        buffer.Render();
        //    }

        //    context.Dispose();
        //    MainWindow.OnRenderingComplete();
        //}

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
