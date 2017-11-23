using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.IO;
using Nebulator.Common;
using Nebulator.Scripting;
using Nebulator.Midi;


namespace Nebulator.UI
{
    /// <summary>
    /// Piano control borrowed from Leslie Sanford with extras.
    /// </summary>
    public partial class Piano : Form
    {
        #region Fields
        const int LOW_NOTE = 21;
        const int HIGH_NOTE = 109;

        /// <summary>All the created piano keys.</summary>
        List<PianoKey> _keys = new List<PianoKey>();

        /// <summary>Map from Keys value to the index in to _keys.</summary>
        Dictionary<Keys, int> _kbdMidi = new Dictionary<Keys, int>();
        #endregion

        #region Events
        public class PianoKeyEventArgs : EventArgs
        {
            public int NoteId { get; set; }
            public bool Down { get; set; }
        }
        public event EventHandler<PianoKeyEventArgs> PianoKeyEvent;
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

            // Load the midi kbd mapping.
            try
            {
                int indexOfMiddleC = _keys.IndexOf(_keys.Where(k => k.NoteId == MidiInterface.MIDDLE_C).First());

                foreach (string l in File.ReadLines(@"Resources\reaper-vkbmap.txt"))
                {
                    List<string> parts = l.SplitByToken(" ");

                    if (parts.Count >= 2 && parts[0] != ";")
                    {
                        char c = parts[0][0];
                        int offset = int.Parse(parts[1]);
                        int note = indexOfMiddleC + offset;

                        switch (c)
                        {
                            case ',': _kbdMidi.Add(Keys.Oemcomma, note); break;
                            case '=': _kbdMidi.Add(Keys.Oemplus, note); break;
                            case '-': _kbdMidi.Add(Keys.OemMinus, note); break;
                            case '/': _kbdMidi.Add(Keys.OemQuestion, note); break;
                            case '.': _kbdMidi.Add(Keys.OemPeriod, note); break;
                            case '\'': _kbdMidi.Add(Keys.OemQuotes, note); break;
                            case '\\': _kbdMidi.Add(Keys.OemPipe, note); break;
                            case ']': _kbdMidi.Add(Keys.OemCloseBrackets, note); break;
                            case '[': _kbdMidi.Add(Keys.OemOpenBrackets, note); break;
                            case '`': _kbdMidi.Add(Keys.Oemtilde, note); break;
                            //case ';': _kbdMidi.Add(Keys.OemSemicolon, note); break; TODO2 support this key?

                            default:
                                if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                                {
                                    _kbdMidi.Add((Keys)c, note);
                                }
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to process midi keyboard file: " + ex.Message);
            }
        }

        /// <summary>
        /// Event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Piano_Resize(object sender, EventArgs e)
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

                pk.PianoKeyEvent += Piano_PianoKeyEvent;
                _keys.Add(pk);
                Controls.Add(pk);
            }
        }
        #endregion

        /// <summary>
        /// Use keyboard to drive piano.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Piano_KeyDown(object sender, KeyEventArgs e)
        {
            if(_kbdMidi.ContainsKey(e.KeyCode))
            {
                PianoKey pk = _keys[_kbdMidi[e.KeyCode]];
                if(!pk.IsPressed)
                {
                    pk.PressPianoKey();
                }
            }
        }

        /// <summary>
        /// Use keyboard to drive piano.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Piano_KeyUp(object sender, KeyEventArgs e)
        {
            if (_kbdMidi.ContainsKey(e.KeyCode))
            {
                PianoKey pk = _keys[_kbdMidi[e.KeyCode]];
                pk.ReleasePianoKey();
            }
        }

        /// <summary>
        /// Pass along an event from a key.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Piano_PianoKeyEvent(object sender, PianoKeyEventArgs e)
        {
            PianoKeyEvent?.Invoke(this, e);
        }

        /// <summary>
        /// Re/draw the keys.
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
        }
    }

    public class PianoKey : Control
    {
        #region Fields
        Piano _owner;
        #endregion

        #region Properties
        public bool IsPressed { get; private set; } = false;
        public bool IsNatural { get; set; } = false;
        public int NoteId { get; set; } = MidiInterface.MIDDLE_C;
        #endregion

        public event EventHandler<Piano.PianoKeyEventArgs> PianoKeyEvent;

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
            PianoKeyEvent?.Invoke(this, new Piano.PianoKeyEventArgs() { NoteId = NoteId, Down = true } );
        }
        
        public void ReleasePianoKey()
        {
            IsPressed = false;
            Invalidate();
            PianoKeyEvent?.Invoke(this, new Piano.PianoKeyEventArgs() { NoteId = NoteId, Down = false });
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
