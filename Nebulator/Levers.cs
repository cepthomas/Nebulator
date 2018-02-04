﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MoreLinq;
using Nebulator.Common;
using Nebulator.Controls;
using Nebulator.Dynamic;


namespace Nebulator
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

            BackColor = UserSettings.TheSettings.BackColor;
        }

        /// <summary>
        /// Initialize the script specific stuff.
        /// </summary>
        /// <param name="levers">Specs for levers from script.</param>
        public void Init(IEnumerable<LeverControlPoint> levers)
        {
            ////// Draw the levers and hook them up.

            // Clean up first.
            foreach(Control c in Controls)
            {
                if(c is Slider)
                {
                    c.Dispose();
                }
            }
            Controls.Clear();

            // Process through our list.
            const int SPACING = 5;
            int x = SPACING;
            int y = SPACING;

            levers.ForEach(l =>
            {
                Slider sl = new Slider()
                {
                    Location = new Point(x, y),
                    Label = l.RefVar.Name,
                    ControlColor = UserSettings.TheSettings.ControlColor,
                    Font = UserSettings.TheSettings.ControlFont,
                    Height = ClientSize.Height - SPACING * 2,
                    Maximum = l.Max,
                    Minimum = l.Min,
                    ResetValue = l.RefVar.Value,
                    Value = l.RefVar.Value,
                    Tag = l.RefVar
                };

                sl.ValueChanged += Lever_ValueChanged;
                Lever_ValueChanged(sl, null); // init it
                Controls.Add(sl);
                x += sl.Width + SPACING;
            });
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