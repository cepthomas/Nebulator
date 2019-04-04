using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using NBagOfTricks.UI;
using Nebulator.Common;
using Nebulator.Device;
using Nebulator.Midi;


namespace Nebulator
{
    /// <summary>
    /// Virtual keyboard borrowed from Leslie Sanford.
    /// </summary>
    public partial class Keyboard : Form, NInput
    {
        #region Events
        /// <inheritdoc />
        public event EventHandler<DeviceLogEventArgs> DeviceLogEvent;

        /// <inheritdoc />
        public event EventHandler<DeviceInputEventArgs> DeviceInputEvent;
        #endregion

        #region Properties
        /// <inheritdoc />
        public string DeviceName { get; private set; } = "";
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public Keyboard()
        {
            InitializeComponent();
        
            // Intercept all keyboard events.
            //KeyPreview = true;
        }

        /// <summary>
        /// Initialize everything.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Keyboard_Load(object sender, EventArgs e)
        {
            // Get the bitmap.
            Bitmap bm = new Bitmap(Properties.Resources.glyphicons_327_piano);

            // Convert to an icon and use for the form's icon.
            Icon = Icon.FromHandle(bm.GetHicon());

            vkey.KeyboardEvent += Vkey_KeyboardEvent;
        }

        /// <inheritdoc />
        public bool Init(string _ = "")
        {
            return true;
        }
        #endregion

        /// <summary>
        /// Handle key events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Vkey_KeyboardEvent(object sender, VirtualKeyboard.KeyboardEventArgs e)
        {
            if (DeviceInputEvent != null)
            {
                Step step = null;

                if (e.Velocity != 0)
                {
                    step = new StepNoteOn()
                    {
                        Device = this,
                        ChannelNumber = e.ChannelNumber,
                        NoteNumber = e.NoteId,
                        Velocity = e.Velocity,
                        VelocityToPlay = e.Velocity,
                        Duration = new Time(0)
                    };
                }
                else
                {
                    step = new StepNoteOff()
                    {
                        Device = this,
                        ChannelNumber = e.ChannelNumber,
                        NoteNumber = e.NoteId,
                        Velocity = 0,
                    };
                }

                DeviceInputEvent?.Invoke(this, new DeviceInputEventArgs() { Step = step });
                LogMsg(DeviceLogCategory.Recv, step.ToString());
            }


        }


        /// <summary>Ask host to do something with this.</summary>
        /// <param name="cat"></param>
        /// <param name="msg"></param>
        void LogMsg(DeviceLogCategory cat, string msg)
        {
            DeviceLogEvent?.Invoke(this, new DeviceLogEventArgs() { DeviceLogCategory = cat, Message = msg });
        }


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
    }
}


/*


using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.IO;
using Nebulator.Common;
using Nebulator.Device;
using Nebulator.Midi;

namespace Nebulator.VirtualKeyboard
{
    /// <summary>
    /// Virtual keyboard control borrowed from Leslie Sanford with extras.
    /// </summary>
    public partial class VKeyboard : Form, NInput
    {
        #region Constants
        const int LOW_NOTE = 21;
        const int HIGH_NOTE = 109;
        const int MIDDLE_C = 60;
        #endregion


        #region User input handlers
        /// <summary>
        /// Use alpha keyboard to drive piano.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Keyboard_KeyDown(object sender, KeyEventArgs e)
        {
            if (!_keyDown)
            {
                if (_keyMap.ContainsKey(e.KeyCode))
                {
                    VKey pk = _keys[_keyMap[e.KeyCode]];
                    if (!pk.IsPressed)
                    {
                        pk.PressVKey();
                        e.Handled = true;
                    }
                }

                _keyDown = true;
            }
        }

        /// <summary>
        /// Use alpha keyboard to drive piano.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Keyboard_KeyUp(object sender, KeyEventArgs e)
        {
            _keyDown = false;

            if (_keyMap.ContainsKey(e.KeyCode))
            {
                VKey pk = _keys[_keyMap[e.KeyCode]];
                pk.ReleaseVKey();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Pass along an event from a virtual key.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Keyboard_VKeyEvent(object sender, VKey.VKeyEventArgs e)
        {
            if (DeviceInputEvent != null)
            {
                Step step = null;

                if (e.Down)
                {
                    step = new StepNoteOn()
                    {
                        Device = this,
                        ChannelNumber = 1,
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
                        Device = this,
                        ChannelNumber = 1,
                        NoteNumber = e.NoteId,
                        Velocity = 0
                    };
                }

                DeviceInputEvent?.Invoke(this, new DeviceInputEventArgs() { Step = step });
                LogMsg(DeviceLogCategory.Recv, step.ToString());
            }
        }

        #endregion
    }
}
*/
