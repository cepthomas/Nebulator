using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Nebulator.Common;


namespace Nebulator.Controls
{
    /// <summary>
    /// Common channel controller.
    /// </summary>
    public partial class ChannelControl : UserControl
    {
        #region Properties
        /// <summary>Corresponding channel object.</summary>
        public Channel? BoundChannel { get; set; }
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public ChannelControl()
        {
            InitializeComponent();
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            sldVolume.Label = Definitions.UNKNOWN_STRING;
        }

        /// <summary>
        /// Initialize the UI from the object.
        /// </summary>
        void ChannelControl_Load(object sender, EventArgs e)
        {
            if(BoundChannel is not null)
            {
                chkSolo.FlatAppearance.CheckedBackColor = UserSettings.TheSettings.SelectedColor;
                chkMute.FlatAppearance.CheckedBackColor = UserSettings.TheSettings.SelectedColor;

                sldVolume.DrawColor = UserSettings.TheSettings.ControlColor;
                sldVolume.DecPlaces = 2;
                sldVolume.Label = BoundChannel.ChannelName;
                sldVolume.Maximum = 1.0;
                sldVolume.Value = BoundChannel.Volume;

                switch (BoundChannel.State)
                {
                    case ChannelState.Normal:
                        chkSolo.Checked = false;
                        chkMute.Checked = false;
                        break;
                    case ChannelState.Solo:
                        chkSolo.Checked = true;
                        chkMute.Checked = false;
                        break;
                    case ChannelState.Mute:
                        chkSolo.Checked = false;
                        chkMute.Checked = true;
                        break;
                }
            }
            else
            {
                //TODO1 error
            }
        }

        /// <summary>
        /// Handles solo and mute.
        /// </summary>
        void Check_Click(object? sender, EventArgs e)
        {
            if(sender is not null && BoundChannel is not null)
            {
                var chk = sender as CheckBox;

                // Fix UI logic.
                if (chk == chkSolo)
                {
                    chkMute.Checked = false;
                }
                else // chkMute
                {
                    chkSolo.Checked = false;
                }

                var st = ChannelState.Normal;
                if (chkSolo.Checked) { st = ChannelState.Solo; }
                else if (chkMute.Checked) { st = ChannelState.Mute; }

                BoundChannel.State = st;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void VolChannel_ValueChanged(object? sender, EventArgs e)
        {
            if (BoundChannel is not null)
            {
                BoundChannel.Volume = sldVolume.Value;
            }
        }
    }
}
