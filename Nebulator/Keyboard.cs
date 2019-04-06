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
    /// Wrapper to turn control into a device.
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

        #region Events
        /// <summary>
        /// Handle key events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Vkey_KeyboardEvent(object sender, VirtualKeyboard.KeyboardEventArgs e)
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
        #endregion

        #region Private functions
        /// <summary>Ask host to do something with this.</summary>
        /// <param name="cat"></param>
        /// <param name="msg"></param>
        void LogMsg(DeviceLogCategory cat, string msg)
        {
            DeviceLogEvent?.Invoke(this, new DeviceLogEventArgs() { DeviceLogCategory = cat, Message = msg });
        }
        #endregion
    }
}
