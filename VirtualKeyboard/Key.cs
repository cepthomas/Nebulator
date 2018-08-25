using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.IO;
using Nebulator.Common;


namespace Nebulator.VirtualKeyboard
{
    public class VKey : Control
    {
        #region Fields
        VKeyboard _owner;
        #endregion

        #region Properties
        public bool IsPressed { get; private set; } = false;
        public bool IsNatural { get; private set; } = false;
        public int NoteId { get; private set; } = 0;
        #endregion

        #region Events
        public event EventHandler<VKeyEventArgs> VKeyEvent;

        public class VKeyEventArgs : EventArgs
        {
            public int NoteId { get; set; }
            public bool Down { get; set; }
        }
        #endregion

        public VKey(VKeyboard owner, bool isNatural, int noteId)
        {
            _owner = owner;
            TabStop = false;
            IsNatural = isNatural;
            NoteId = noteId;
        }

        public void PressVKey()
        {
            IsPressed = true;
            Invalidate();
            VKeyEvent?.Invoke(this, new VKeyEventArgs() { NoteId = NoteId, Down = true });
        }

        public void ReleaseVKey()
        {
            IsPressed = false;
            Invalidate();
            VKeyEvent?.Invoke(this, new VKeyEventArgs() { NoteId = NoteId, Down = false });
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            if (MouseButtons == MouseButtons.Left)
            {
                PressVKey();
            }

            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (IsPressed)
            {
                ReleaseVKey();
            }

            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            PressVKey();

            if (!_owner.Focused)
            {
                _owner.Focus();
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            ReleaseVKey();

            base.OnMouseUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.X < 0 || e.X > Width || e.Y < 0 || e.Y > Height)
            {
                Capture = false;
            }

            base.OnMouseMove(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (IsPressed)
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.SkyBlue), 0, 0, Size.Width, Size.Height);
            }
            else
            {
                e.Graphics.FillRectangle(IsNatural ? new SolidBrush(Color.White) : new SolidBrush(Color.Black), 0, 0, Size.Width, Size.Height);
            }

            e.Graphics.DrawRectangle(Pens.Black, 0, 0, Size.Width - 1, Size.Height - 1);

            base.OnPaint(e);
        }
    }
}
