using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using NBagOfTricks.Utils;
using NBagOfTricks.UI;
using Nebulator.Common;
using Nebulator.Script;


namespace Nebulator
{
    public partial class ScriptControls : UserControl
    {
        #region Fields
        /// <summary>Internal flag.</summary>
        bool _init = false;
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public ScriptControls()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initialize the controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScriptControls_Load(object sender, EventArgs e)
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
        /// <param name="displays">Specs for meters from script.</param>
        public void Init(IEnumerable<NController> levers, IEnumerable<NDisplay> displays)
        {
            _init = false;

            ////// Draw the levers and meters and hook them up.

            // Clean up old ones first.
            foreach(Control c in Controls)
            {
                if(c is Slider || c is Meter)
                {
                    c.Dispose();
                }
            }
            Controls.Clear();

            // Process through our list.
            const int WIDTH = 80;
            const int SPACING = 5;
            int x = SPACING;
            int y = SPACING;

            levers.ForEach(l =>
            {
                Slider slider = new Slider()
                {
                    Location = new Point(x, y),
                    Label = l.BoundVar.Name,
                    DrawColor = UserSettings.TheSettings.ControlColor,
                    Font = UserSettings.TheSettings.ControlFont,
                    Height = ClientSize.Height - SPACING * 2,
                    Width = WIDTH,
                    Maximum = l.BoundVar.Max,
                    Minimum = l.BoundVar.Min,
                    ResetValue = l.BoundVar.Value,
                    Value = l.BoundVar.Value,
                    Tag = l.BoundVar
                };

                slider.ValueChanged += Lever_ValueChanged;
                Lever_ValueChanged(slider, null); // init it
                Controls.Add(slider);
                x += slider.Width + SPACING;
            });

            displays.ForEach(d =>
            {
                Meter meter = new Meter()
                {
                    Location = new Point(x, y),
                    Label = d.BoundVar.Name,
                    DrawColor = UserSettings.TheSettings.ControlColor,
                    Font = UserSettings.TheSettings.ControlFont,
                    Height = ClientSize.Height - SPACING * 2,
                    Width = WIDTH,
                    Maximum = d.BoundVar.Max,
                    Minimum = d.BoundVar.Min,
                };

                d.BoundVar.Tag = meter;

                switch (d.DisplayType)
                {
                    case DisplayType.LinearMeter:
                        meter.MeterType = MeterType.Linear;
                        break;

                    case DisplayType.LogMeter:
                        meter.MeterType = MeterType.Log;
                        break;

                    case DisplayType.Chart:
                        meter.MeterType = MeterType.ContinuousLine;
                        break;
                }

                d.BoundVar.ValueChangeEvent += Meter_ValueChangeEvent;
                Controls.Add(meter);
                x += meter.Width + SPACING;
            });

            _init = true;
        }

        /// <summary>
        /// Meter bound value changed so update control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Meter_ValueChangeEvent(object sender, EventArgs e)
        {
            // Dig out the control.
            NVariable var = sender as NVariable;
            Meter m = var.Tag as Meter;
            m.AddValue(var.Value);
        }

        /// <summary>
        /// One of the lever controls was changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Lever_ValueChanged(object sender, EventArgs e)
        {
            if(_init && sender is Slider)
            {
                // Update the bound var and report to the master.
                Slider sl = sender as Slider;
                NVariable refVar = sl.Tag as NVariable;
                refVar.Value = sl.Value; // This triggers any hooked script handlers.
            }
        }
    }
}
