using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MoreLinq;
using Nebulator.Common;
using Nebulator.Model;
using Nebulator.Controls;


namespace Nebulator.UI
{
    public partial class Levers : UserControl
    {
        #region Events
        /// <summary>Reporting a change to listeners.</summary>
        public event EventHandler<LeverChangeEventArgs> LeverChangeEvent;
        public class LeverChangeEventArgs : EventArgs
        {
            public Variable RefVar { get; set; } = null;
        }
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public Levers()
        {
            InitializeComponent();
            toolStrip.Renderer = new Common.CheckBoxRenderer(); // for checked color.
        }

        /// <summary>
        /// Initialize the controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Levers_Load(object sender, EventArgs e)
        {
            // Use double buffering to reduce flicker.
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            UpdateStyles();

            BackColor = Globals.UserSettings.BackColor;
            btnEnableLevers.Image = Utils.ColorizeBitmap(btnEnableLevers.Image);
        }

        /// <summary>
        /// Initialize the script specific stuff.
        /// </summary>
        /// <param name="surface">Where to create controls.</param>
        public void Init(UserControl surface)
        {
            ////// Draw the levers and hook them up.
            splitContainer1.Panel1.Controls.Clear();

            // Process through our list.
            const int SPACING = 5;
            int x = SPACING;
            int y = SPACING;

            Globals.Dynamic.Levers.Values.ForEach(l =>
            {
                Slider sl = new Slider()
                {
                    Location = new Point(x, y),
                    Label = l.RefVar.Name,
                    ControlColor = Globals.UserSettings.ControlColor,
                    Font = Globals.UserSettings.ControlFont,
                    Height = splitContainer1.Panel1.ClientSize.Height - SPACING * 2,
                    Maximum = l.Max,
                    Minimum = l.Min,
                    ResetValue = l.RefVar.Value,
                    Value = l.RefVar.Value,
                    Tag = l.RefVar
                };

                sl.ValueChanged += Lever_ValueChanged;
                Lever_ValueChanged(sl, null); // init it
                splitContainer1.Panel1.Controls.Add(sl);
                x += sl.Width + SPACING;
            });

            ///// Create the surface panel.
            splitContainer1.Panel2.Controls.Clear();
            surface.Dock = DockStyle.Fill;
            splitContainer1.Panel2.Controls.Add(surface);
        }

        /// <summary>
        /// One of the lever controls was changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Lever_ValueChanged(object sender, EventArgs e)
        {
            if(sender is Slider)
            {
                // Update the bound var and report to the master.
                Slider sl = sender as Slider;
                Variable refVar = sl.Tag as Variable;
                refVar.Value = sl.Value;

                LeverChangeEvent?.Invoke(this, new LeverChangeEventArgs() { RefVar = refVar });
            }
        }
    }
}
