using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Nebulator.Model;
using Nebulator.Common;
using Nebulator.Engine;


namespace Nebulator.UI
{
    /// <summary>
    /// Common track controller.
    /// </summary>
    public partial class TrackControl : UserControl
    {
        #region Properties
        /// <summary>
        /// Corresponding track object.
        /// </summary>
        public Track TrackInfo { get; set; }

        /// <summary>
        /// Track state.
        /// </summary>
        public TrackState State { get; set; } = TrackState.Normal;
        #endregion

        #region Events
        /// <summary>
        /// User edit event.
        /// </summary>
        public event EventHandler<TrackChangeEventArgs> TrackChangeEvent;
        public class TrackChangeEventArgs : EventArgs
        {
            public string What { get; set; } = "";
        }
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public TrackControl()
        {
            InitializeComponent();
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            State = TrackState.Normal;
        }

        /// <summary>
        /// Initialize the UI from the object.
        /// </summary>
        private void TrackControl_Load(object sender, EventArgs e)
        {
            chkSolo.FlatAppearance.CheckedBackColor = Globals.UserSettings.SelectedColor;
            chkMute.FlatAppearance.CheckedBackColor = Globals.UserSettings.SelectedColor;
            sldVolume.ControlColor = Globals.UserSettings.ControlColor;
            sldVolume.Font = Globals.UserSettings.ControlFont;
            sldVolume.Label = TrackInfo.Name;
            sldVolume.Maximum = Midi.MAX_MIDI_VOLUME;
            sldVolume.Value = TrackInfo.Volume;

            sldVolume.ValueChanged += VolTrack_ValueChanged;
        }

        /// <summary>
        /// Handles solo and mute functions.
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
                State = TrackState.Mute;
            }
            else if (chkSolo.Checked)
            {
                State = TrackState.Solo;
            }
            else
            {
                State = TrackState.Normal;
            }

            TrackChangeEvent?.Invoke(this, new TrackChangeEventArgs() { What = "TrackState" });
        }

        /// <summary>
        /// 
        /// </summary>
        private void VolTrack_ValueChanged(object sender, EventArgs e)
        {
            TrackInfo.Volume = sldVolume.Value;
        }
    }
}
