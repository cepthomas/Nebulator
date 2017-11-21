using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using Nebulator.Common;
using Nebulator.Scripting;


namespace Nebulator.UI
{
    /// <summary>
    /// Piano control borrowed from Leslie Sanford. TODO1 add support for keyboard.
    /// </summary>
    public partial class Piano : Form
    {
        #region Fields
        const int LOW_NOTE = 21;
        const int HIGH_NOTE = 109;
        List<PianoKey> _keys = new List<PianoKey>();
        #endregion

        #region Events
        public class PianoKeyEventArgs : EventArgs
        {
            public int NoteId { get; set; }
        }
        public event EventHandler<PianoKeyEventArgs> PianoKeyDown;
        public event EventHandler<PianoKeyEventArgs> PianoKeyUp;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public Piano()
        {
            CreatePianoKeys();
            InitializeComponent();
        }

        /// <summary>
        /// Initialize everything.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Piano_Load(object sender, EventArgs e)
        {
            DrawPianoKeys();
        }

        /// <summary>
        /// Create the key controls.
        /// </summary>
        private void CreatePianoKeys()
        {
            _keys.Clear();

            for (int i = 0; i < HIGH_NOTE - LOW_NOTE; i++)
            {
                int noteId = i + LOW_NOTE;
                Note note = new Note(noteId);

                PianoKey pk;
                if (NoteUtils.IsNatural(note.Root))
                {
                    pk = new PianoKey(this, true, noteId);
                }
                else
                {
                    pk = new PianoKey(this, false, noteId);
                    pk.BringToFront();
                }
                _keys.Add(pk);
                Controls.Add(pk);
            }
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        void DrawPianoKeys()
        {
            int whiteKeyWidth = Width / _keys.Count(k => k.IsNatural);
            int blackKeyWidth = (int)(whiteKeyWidth * 0.6);
            int blackKeyHeight = (int)(Height * 0.5);
            int offset = whiteKeyWidth - blackKeyWidth / 2;

            int w = 0;

            for (int i = 0; i < _keys.Count; i++)
            {
                PianoKey pk = _keys[i];

                if (pk.IsNatural)
                {
                    pk.Height = Height;
                    pk.Width = whiteKeyWidth;
                    pk.Location = new Point(w * whiteKeyWidth, 0);
                    w++;
                }
                else
                {
                    pk.Height = blackKeyHeight;
                    pk.Width = blackKeyWidth;
                    pk.Location = new Point(offset + (w - 1) * whiteKeyWidth);
                    pk.BringToFront();
                }
            }



            //int n = 0;
            //int w = 0;

            //while (n < _keys.Count)
            //{
            //    Note note = new Note(_keys[n].noteId);

            //    if (NoteUtils.IsNatural(note.Root))
            //    {
            //        _keys[n].Height = Height;
            //        _keys[n].Width = whiteKeyWidth;
            //        _keys[n].Location = new Point(w * whiteKeyWidth, 0);
            //        w++;
            //    }
            //    else
            //    {
            //        _keys[n].Height = blackKeyHeight;
            //        _keys[n].Width = blackKeyWidth;
            //        _keys[n].Location = new Point(offset + (w - 1) * whiteKeyWidth);
            //        _keys[n].BringToFront();
            //    }
            //    n++;
            //}
        }

        public void PressPianoKey(int noteId)
        {
            _keys[noteId - LOW_NOTE].PressPianoKey();
        }

        public void ReleasePianoKey(int noteId)
        {
            _keys[noteId - LOW_NOTE].ReleasePianoKey();
        }

        protected override void OnResize(EventArgs e)
        {
            DrawPianoKeys();
            base.OnResize(e);
        }

        public virtual void OnPianoKeyDown(PianoKeyEventArgs e)
        {
            PianoKeyDown?.Invoke(this, e);
        }

        public virtual void OnPianoKeyUp(PianoKeyEventArgs e)
        {
            PianoKeyUp?.Invoke(this, e);
        }
    }


    public class PianoKey : Control
    {
        Piano _owner; // ?????????

        public bool IsPressed { get; private set; } = false;
        public bool IsNatural { get; set; } = false;
        public int NoteId { get; set; } = 60;

        public PianoKey(Piano owner, bool isNatural, int noteId)
        {
            _owner = owner;
            TabStop = false;
            IsNatural = isNatural;
            NoteId = noteId;
        }

        public void PressPianoKey()
        {
            IsPressed = true;
            Invalidate();
            _owner.OnPianoKeyDown(new Piano.PianoKeyEventArgs() { NoteId = NoteId } );
        }

        public void ReleasePianoKey()
        {
            IsPressed = false;
            Invalidate();
            _owner.OnPianoKeyUp(new Piano.PianoKeyEventArgs() { NoteId = NoteId });
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            if (MouseButtons == MouseButtons.Left)
            {
                PressPianoKey();
            }

            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (IsPressed)
            {
                ReleasePianoKey();
            }

            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            PressPianoKey();

            if (!_owner.Focused)
            {
                _owner.Focus();
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            ReleasePianoKey();

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
