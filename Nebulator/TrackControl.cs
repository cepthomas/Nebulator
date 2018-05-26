using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Nebulator.Common;
using Nebulator.Script;


namespace Nebulator
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
        public NTrack BoundTrack { get; set; }
        #endregion

        #region Events
        /// <summary>
        /// User changed something.
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
        }

        /// <summary>
        /// Initialize the UI from the object.
        /// </summary>
        private void TrackControl_Load(object sender, EventArgs e)
        {
            chkSolo.FlatAppearance.CheckedBackColor = UserSettings.TheSettings.SelectedColor;
            chkMute.FlatAppearance.CheckedBackColor = UserSettings.TheSettings.SelectedColor;
            sldVolume.ControlColor = UserSettings.TheSettings.ControlColor;
            sldVolume.Font = UserSettings.TheSettings.ControlFont;
            sldVolume.Label = BoundTrack.Name;
            sldVolume.Maximum = 200;
            sldVolume.Value = BoundTrack.Volume;

            sldVolume.ValueChanged += VolTrack_ValueChanged;
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
                BoundTrack.State = TrackState.Mute;
            }
            else if (chkSolo.Checked)
            {
                BoundTrack.State = TrackState.Solo;
            }
            else
            {
                BoundTrack.State = TrackState.Normal;
            }

            TrackChangeEvent?.Invoke(this, new TrackChangeEventArgs() { What = "TrackState" });
        }

        /// <summary>
        /// 
        /// </summary>
        private void VolTrack_ValueChanged(object sender, EventArgs e)
        {
            BoundTrack.Volume = sldVolume.Value;
        }
    }
}
