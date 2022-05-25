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
using NLog;
using NBagOfUis;
using Nebulator.Common;
using Nebulator.Midi;
using MidiLib;


namespace Nebulator.UI
{
    /// <summary>
    /// Wrapper to turn control into a device.
    /// </summary>
    public partial class Keyboard : Form, IInputDevice
    {
        #region Fields
        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.GetLogger("Keyboard");
        #endregion

        #region Events
        /// <inheritdoc />
        public event EventHandler<DeviceInputEventArgs>? DeviceInputEvent;
        #endregion

        #region Properties
        /// <inheritdoc />
        public string DeviceName { get; private set; } = "";

        /// <inheritdoc />
        public DeviceType DeviceType => DeviceType.Vkey;
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

            StartPosition = FormStartPosition.Manual;
            TopMost = false;
            Size = new Size(UserSettings.TheSettings.KeyboardFormGeometry.Width, UserSettings.TheSettings.KeyboardFormGeometry.Height);
            Location = new Point(UserSettings.TheSettings.KeyboardFormGeometry.X, UserSettings.TheSettings.KeyboardFormGeometry.Y);
            vkey.KeyboardEvent += Vkey_KeyboardEvent;
        }

        /// <inheritdoc />
        public bool Init()
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
        void Vkey_KeyboardEvent(object? sender, VirtualKeyboard.KeyboardEventArgs e)
        {
            if (DeviceInputEvent is not null)
            {
                Step step;

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
                if(UserSettings.TheSettings.MonitorInput)
                {
                    _logger.Trace($"{TraceCat.RCV} KbdIn:{step}");
                }
            }
        }
        #endregion
    }
}
