using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Nebulator.Common;
using Nebulator.Script;


namespace Nebulator
{
    /// <summary>
    /// Common channel controller.
    /// </summary>
    public partial class ChannelControl : UserControl
    {
        #region Properties
        /// <summary>
        /// Corresponding channel object.
        /// </summary>
        public NChannel BoundChannel { get; set; }
        #endregion

        #region Events
        /// <summary>
        /// User changed something.
        /// </summary>
        public event EventHandler ChannelChangeEvent;
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public ChannelControl()
        {
            InitializeComponent();
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }

        /// <summary>
        /// Initialize the UI from the object.
        /// </summary>
        private void ChannelControl_Load(object sender, EventArgs e)
        {
            chkSolo.FlatAppearance.CheckedBackColor = UserSettings.TheSettings.SelectedColor;
            chkMute.FlatAppearance.CheckedBackColor = UserSettings.TheSettings.SelectedColor;
            sldVolume.ControlColor = UserSettings.TheSettings.ControlColor;
            sldVolume.Font = UserSettings.TheSettings.ControlFont;
            sldVolume.Label = BoundChannel.Name;
            sldVolume.Maximum = 200;
            sldVolume.Value = BoundChannel.Volume;

            sldVolume.ValueChanged += VolChannel_ValueChanged;
        }

        /// <summary>
        /// Handles solo and mute.
        /// </summary>
        private void Check_Click(object sender, EventArgs e)
        {
            CheckBox chk = sender as CheckBox;

            if (chk == chkSolo)
            {
                chkMute.Checked = false;
            }
            else
            {
                chkSolo.Checked = false;
            }

            if (chkMute.Checked)
            {
                BoundChannel.State = ChannelState.Mute;
            }
            else if (chkSolo.Checked)
            {
                BoundChannel.State = ChannelState.Solo;
            }
            else
            {
                BoundChannel.State = ChannelState.Normal;
            }

            ChannelChangeEvent?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// 
        /// </summary>
        private void VolChannel_ValueChanged(object sender, EventArgs e)
        {
            BoundChannel.Volume = sldVolume.Value;
            ChannelChangeEvent?.Invoke(this, new EventArgs());
        }
    }
}
