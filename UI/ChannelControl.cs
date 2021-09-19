﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Nebulator.Common;
using Nebulator.Script;


namespace Nebulator.UI
{
    /// <summary>
    /// Common channel controller.
    /// </summary>
    public partial class ChannelControl : UserControl
    {
        #region Properties
        /// <summary>Corresponding channel object.</summary>
        //public NChannel BoundChannel { get; set; }


        public ChannelState State
        {
            get
            {
                var st = ChannelState.Normal;
                if (chkSolo.Checked) { st = ChannelState.Solo; }
                else if (chkMute.Checked) { st = ChannelState.Mute; }
                return st;
            }
            set
            {
                switch(value)
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
        }

        public string Label { get { return sldVolume.Label; } set { sldVolume.Label = value; } }

        public double Volume { get { return sldVolume.Value; } set { sldVolume.Value = value; } }
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
            sldVolume.Label = Definitions.UNKNOWN_STRING;
        }

        /// <summary>
        /// Initialize the UI from the object.
        /// </summary>
        private void ChannelControl_Load(object sender, EventArgs e)
        {
            chkSolo.FlatAppearance.CheckedBackColor = UserSettings.TheSettings.SelectedColor;
            chkMute.FlatAppearance.CheckedBackColor = UserSettings.TheSettings.SelectedColor;
            sldVolume.DrawColor = UserSettings.TheSettings.ControlColor;
            //sldVolume.Font = UserSettings.TheSettings.ControlFont;
            sldVolume.DecPlaces = 2;
            //sldVolume.Label = BoundChannel.Name;
            sldVolume.Maximum = 1.0;
            //sldVolume.Value = BoundChannel.Volume;

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
            else // chkMute
            {
                chkSolo.Checked = false;
            }

            ChannelChangeEvent?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// 
        /// </summary>
        private void VolChannel_ValueChanged(object sender, EventArgs e)
        {
  //          BoundChannel.Volume = sldVolume.Value;
            ChannelChangeEvent?.Invoke(this, new EventArgs());
        }
    }
}