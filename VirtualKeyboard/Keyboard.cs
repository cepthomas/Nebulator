using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.IO;
using Nebulator.Common;
using Nebulator.Comm;


namespace Nebulator.VirtualKeyboard
{
    /// <summary>
    /// Virtual keyboard control borrowed from Leslie Sanford with extras.
    /// </summary>
    public partial class VKeyboard : Form, ICommInput
    {
        #region Events
        /// <inheritdoc />
        public event EventHandler<CommLogEventArgs> CommLogEvent;

        /// <inheritdoc />
        public event EventHandler<CommInputEventArgs> CommInputEvent;
        #endregion

        #region Fields
        const int LOW_NOTE = 21;
        const int HIGH_NOTE = 109;
        const int MIDDLE_C = 60;

        /// <summary>All the created piano keys.</summary>
        List<VKey> _keys = new List<VKey>();

        /// <summary>Map from Keys value to the index in _keys.</summary>
        Dictionary<Keys, int> _keysMap = new Dictionary<Keys, int>();
        #endregion

        #region Properties
        /// <inheritdoc />
        public string CommName { get; set; } = "Virtual Keyboard";

        /// <inheritdoc />
        public CommCaps Caps { get; set; }

        /// <inheritdoc />
        public bool Monitor { get; set; }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public VKeyboard()
        {
            CreateKeys();
            InitializeComponent();

            Caps = new CommCaps() //TODOX common?
            {
                NumChannels = 16,
                MinVolume = 0,
                MaxVolume = 127,
                MinNote = 0,
                MaxNote = 127,
                MinControllerValue = 0,
                MaxControllerValue = 127,
                MinPitchValue = 0,
                MaxPitchValue = 16383
            };
        }

        /// <summary>
        /// Initialize everything.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Keyboard_Load(object sender, EventArgs e)
        {
            CommLogEvent?.Invoke(this, new CommLogEventArgs() { Category = CommLogEventArgs.LogCategory.Info, Message = "Load VKeyboard" });
        }

        /// <inheritdoc />
        public bool Init()
        {
            bool ret = true;

            DrawKeys();

            // Load the midi kbd mapping.
            try
            {
                int indexOfMiddleC = _keys.IndexOf(_keys.Where(k => k.NoteId == MIDDLE_C).First());

                string fpath = Path.Combine(Utils.GetExeDir(), @"Resources\reaper-vkbmap.txt");//TODOX 
                foreach (string l in File.ReadLines(fpath))
                {
                    List<string> parts = l.SplitByToken(" ");

                    if (parts.Count >= 2 && parts[0] != ";")
                    {
                        string key = parts[0];
                        char ch = key[0];
                        int offset = int.Parse(parts[1]);
                        int note = indexOfMiddleC + offset;

                        switch (key)
                        {
                            case ",": _keysMap.Add(Keys.Oemcomma, note); break;
                            case "=": _keysMap.Add(Keys.Oemplus, note); break;
                            case "-": _keysMap.Add(Keys.OemMinus, note); break;
                            case "/": _keysMap.Add(Keys.OemQuestion, note); break;
                            case ".": _keysMap.Add(Keys.OemPeriod, note); break;
                            case "\'": _keysMap.Add(Keys.OemQuotes, note); break;
                            case "\\": _keysMap.Add(Keys.OemPipe, note); break;
                            case "]": _keysMap.Add(Keys.OemCloseBrackets, note); break;
                            case "[": _keysMap.Add(Keys.OemOpenBrackets, note); break;
                            case "`": _keysMap.Add(Keys.Oemtilde, note); break;
                            case ";;": _keysMap.Add(Keys.OemSemicolon, note); break;

                            default:
                                if ((ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9'))
                                {
                                    _keysMap.Add((Keys)ch, note);
                                }
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CommLogEvent?.Invoke(this, new CommLogEventArgs() { Category = CommLogEventArgs.LogCategory.Error, Message = $"Unable to process midi keyboard file:{ex.Message}" });
                ret = false;
            }

            CommLogEvent?.Invoke(this, new CommLogEventArgs() { Category = CommLogEventArgs.LogCategory.Info, Message = "Init VKeyboard" });
            Show();

            return ret;
        }

        /// <summary>
        /// Event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Keyboard_Resize(object sender, EventArgs e)
        {
            DrawKeys();
        }
        #endregion

        #region Public functions
        /// <inheritdoc />
        public void Start()
        {
        }

        /// <inheritdoc />
        public void Stop()
        {
        }

        /// <inheritdoc />
        public void Housekeep()
        {
        }
        #endregion

        #region User input handlers
        /// <summary>
        /// Use alpha keyboard to drive piano.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Keyboard_KeyDown(object sender, KeyEventArgs e)
        {
            if (_keysMap.ContainsKey(e.KeyCode))
            {
                VKey pk = _keys[_keysMap[e.KeyCode]];
                if (!pk.IsPressed)
                {
                    pk.PressVKey();
                }
            }
        }

        /// <summary>
        /// Use alpha keyboard to drive piano.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Keyboard_KeyUp(object sender, KeyEventArgs e)
        {
            if (_keysMap.ContainsKey(e.KeyCode))
            {
                VKey pk = _keys[_keysMap[e.KeyCode]];
                pk.ReleaseVKey();
            }
        }

        /// <summary>
        /// Pass along an event from a virtual key.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Keyboard_VKeyEvent(object sender, VKey.VKeyEventArgs e)
        {
            if(CommInputEvent != null)
            {
                Step step = null;

                if (e.Down)
                {
                    step = new StepNoteOn()
                    {
                        ChannelNumber = -1, // caller will fill? TODOX
                        NoteNumber = e.NoteId,
                        Velocity = 100,
                        VelocityToPlay = 100,
                        Duration = new Time(0)
                    };
                }
                else // up
                {
                    step = new StepNoteOff()
                    {
                        ChannelNumber = -1, // caller will fill? TODOX
                        NoteNumber = e.NoteId,
                        Velocity = 0
                    };
                }

                CommInputEvent.Invoke(this, new CommInputEventArgs() { Step = step });
            }
        }
        #endregion

        #region Drawing
        /// <summary>
        /// Create the key controls.
        /// </summary>
        void CreateKeys()
        {
            _keys.Clear();

            for (int i = 0; i < HIGH_NOTE - LOW_NOTE; i++)
            {
                int noteId = i + LOW_NOTE;
                VKey pk;
                if (NoteUtils.IsNatural(noteId))
                {
                    pk = new VKey(this, true, noteId);
                }
                else
                {
                    pk = new VKey(this, false, noteId);
                    pk.BringToFront();
                }

                pk.VKeyEvent += Keyboard_VKeyEvent;
                _keys.Add(pk);
                Controls.Add(pk);
            }
        }

        /// <summary>
        /// Re/draw the keys.
        /// </summary>
        void DrawKeys()
        {
            int whiteKeyWidth = Width / _keys.Count(k => k.IsNatural);
            int blackKeyWidth = (int)(whiteKeyWidth * 0.6);
            int blackKeyHeight = (int)(Height * 0.5);
            int offset = whiteKeyWidth - blackKeyWidth / 2;

            int w = 0;

            for (int i = 0; i < _keys.Count; i++)
            {
                VKey pk = _keys[i];

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
        #endregion
    }
}
