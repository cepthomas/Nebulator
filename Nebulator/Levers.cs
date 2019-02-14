using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Nebulator.Common;
using Nebulator.Controls;
using Nebulator.Script;


namespace Nebulator
{
    public partial class Levers : UserControl
    {
        #region Fields
        /// <summary>Internal flag.</summary>
        bool _init = true;
        #endregion

        #region Events
        /// <summary>Reporting a change to listeners.</summary>
        public event EventHandler<LeverChangeEventArgs> LeverChangeEvent;

        public class LeverChangeEventArgs : EventArgs
        {
            public NVariable BoundVar { get; set; } = null;
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
        public void Init(IEnumerable<NController> levers)
        {
            _init = true;

            ////// Draw the levers and hook them up.

            // Clean up old ones first.
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
                    Label = l.BoundVar.Name,
                    ControlColor = UserSettings.TheSettings.ControlColor,
                    Font = UserSettings.TheSettings.ControlFont,
                    Height = ClientSize.Height - SPACING * 2,
                    Maximum = l.BoundVar.Max,
                    Minimum = l.BoundVar.Min,
                    ResetValue = l.BoundVar.Value,
                    Value = l.BoundVar.Value,
                    Tag = l.BoundVar
                };

                sl.ValueChanged += Lever_ValueChanged;
                Lever_ValueChanged(sl, null); // init it
                Controls.Add(sl);
                x += sl.Width + SPACING;
            });

            _init = false;
        }

        /// <summary>
        /// One of the lever controls was changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Lever_ValueChanged(object sender, EventArgs e)
        {
            if(!_init && sender is Slider)
            {
                // Update the bound var and report to the master.
                Slider sl = sender as Slider;
                NVariable refVar = sl.Tag as NVariable;
                refVar.Value = sl.Value; // This triggers any hooked script handlers.

                LeverChangeEvent?.Invoke(this, new LeverChangeEventArgs() { BoundVar = refVar });
            }
        }
    }
}
