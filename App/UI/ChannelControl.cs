﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MidiLib;
using Nebulator.Common;


namespace Nebulator.UI
{
    /// <summary>
    /// Common channel controller.
    /// </summary>
    public partial class ChannelControl_XXX : UserControl
    {
        #region Properties
        /// <summary>Corresponding channel object.</summary>
        public Channel BoundChannel { get; set; } = new();
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public ChannelControl_XXX()
        {
            InitializeComponent();
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            chkSolo.FlatAppearance.CheckedBackColor = Color.Green;
            chkMute.FlatAppearance.CheckedBackColor = Color.Red;

            //sldVolume.DrawColor = UserSettings.TheSettings.ControlColor;
            //sldVolume.BackColor = UserSettings.TheSettings.BackColor;
            sldVolume.Resolution = 0.05;
            sldVolume.Maximum = 1.0;
        }

        /// <summary>
        /// Post construct init.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            sldVolume.Label = BoundChannel.ChannelName;
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

            base.OnLoad(e);
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
