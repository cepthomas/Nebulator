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
using NBagOfTricks.UI;
using Nebulator.Common;
using Nebulator.Midi;


namespace Nebulator.Controls
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
        public event EventHandler<DeviceInputEventArgs> DeviceInputEvent;
        #endregion

        #region Properties
        /// <inheritdoc />
        public string DeviceName { get; private set; } = "";

        /// <inheritdoc />
        public DeviceType DeviceType => DeviceType.VkeyIn;
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
            //// Get the bitmap. TODO2
            //Bitmap bm = new Bitmap(App.Properties.Resources.glyphicons_327_piano);

            //// Convert to an icon and use for the form's icon.
            //Icon = Icon.FromHandle(bm.GetHicon());

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
                if(UserSettings.TheSettings.MonitorInput)
                {
                    _logger.Trace($"{TraceCat.RCV} KbdIn:{step}");
                }
            }
        }
        #endregion
    }
}
