using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Nebulator.Common;
using Nebulator.Model;


namespace Nebulator.UI
{
    /// <summary>
    /// Piano control borrowed from Leslie Sanford.
    /// </summary>
    public partial class Piano : Form
    {
        public enum KeyType { White, Black }

        private int _lowNoteId = 21;
        private int _highNoteId = 109;
        private PianoKey[] _keys = null;
        private int _whiteKeyCount;

        public event EventHandler<PianoKeyEventArgs> PianoKeyDown;
        public event EventHandler<PianoKeyEventArgs> PianoKeyUp;

        public Piano()
        {
            CreatePianoKeys();
            InitializeComponent();
        }

        private void Piano_Load(object sender, EventArgs e)
        {
            InitializePianoKeys();
        }

        private void CreatePianoKeys()
        {
            _keys = new PianoKey[_highNoteId - _lowNoteId];

            _whiteKeyCount = 0;

            for (int i = 0; i < _keys.Length; i++)
            {
                int noteId = i + _lowNoteId;

                Note note = new Note(noteId);

                if(NoteUtils.IsNatural(note.Root))
                {
                    _whiteKeyCount++;
                    _keys[i] = new PianoKey(this, KeyType.White, noteId);
                }
                else
                {
                    _keys[i] = new PianoKey(this, KeyType.Black, noteId);
                    _keys[i].BringToFront();
                }

                Controls.Add(_keys[i]);
            }
        }

        private void InitializePianoKeys()
        {
            int whiteKeyWidth = Width / _whiteKeyCount;
            int blackKeyWidth = (int)(whiteKeyWidth * 0.6);
            int blackKeyHeight = (int)(Height * 0.5);
            int offset = whiteKeyWidth - blackKeyWidth / 2;
            int n = 0;
            int w = 0;

            while (n < _keys.Length)
            {
                Note note = new Note(_keys[n].NoteID);

                if (NoteUtils.IsNatural(note.Root))
                {
                    _keys[n].Height = Height;
                    _keys[n].Width = whiteKeyWidth;
                    _keys[n].Location = new Point(w * whiteKeyWidth, 0);
                    w++;
                }
                else
                {
                    _keys[n].Height = blackKeyHeight;
                    _keys[n].Width = blackKeyWidth;
                    _keys[n].Location = new Point(offset + (w - 1) * whiteKeyWidth);
                    _keys[n].BringToFront();
                }
                n++;
            }
        }

        public void PressPianoKey(int noteID)
        {
            _keys[noteID - _lowNoteId].PressPianoKey();
        }

        public void ReleasePianoKey(int noteID)
        {
            _keys[noteID - _lowNoteId].ReleasePianoKey();
        }

        protected override void OnResize(EventArgs e)
        {
            InitializePianoKeys();
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

    public class PianoKeyEventArgs : EventArgs
    {
        public int NoteID { get; set; }
    }


    public class PianoKey : Control
    {
        Piano _owner;

        public bool IsPressed { get; private set; }
        public Piano.KeyType KeyType { get; set; }
        public int NoteID { get; set; } = 60;

        public PianoKey(Piano owner, Piano.KeyType kt, int noteId)
        {
            _owner = owner;
            TabStop = false;
            KeyType = kt;
            NoteID = noteId;
        }

        public void PressPianoKey()
        {
            IsPressed = true;
            Invalidate();
            _owner.OnPianoKeyDown(new PianoKeyEventArgs() { NoteID = NoteID } );
        }

        public void ReleasePianoKey()
        {
            IsPressed = false;
            Invalidate();
            _owner.OnPianoKeyUp(new PianoKeyEventArgs() { NoteID = NoteID });
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
                e.Graphics.FillRectangle(KeyType == Piano.KeyType.White ? new SolidBrush(Color.White) : new SolidBrush(Color.Black), 0, 0, Size.Width, Size.Height);
            }

            e.Graphics.DrawRectangle(Pens.Black, 0, 0, Size.Width - 1, Size.Height - 1);

            base.OnPaint(e);
        }
    }
}
