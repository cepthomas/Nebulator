using Nebulator.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NBagOfTricks;
using NAudio.Midi;
using NAudio.Wave;

namespace Nebulator.UI
{
    public partial class SettingsEditor : Form
    {
        /// <summary>The client settings.</summary>
        public UserSettings Settings { get; set; } = new();

        /// <summary>Settings to edit.</summary>
        UserSettings _settingsTemp = new();

        /// <summary>Detect edits..</summary>
        bool _changed = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SettingsEditor()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Init stuff.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingsEditor_Load(object sender, EventArgs e)
        {
            StartPosition = FormStartPosition.Manual;
            Location = new Point(200, 200);

            // Make a copy for editing. Probably should use binding?
            _settingsTemp.AutoCompile = Settings.AutoCompile;
            _settingsTemp.WorkPath = Settings.WorkPath;
            _settingsTemp.CpuMeter = Settings.CpuMeter;
            _settingsTemp.IgnoreWarnings = Settings.IgnoreWarnings;
            _settingsTemp.BackColor = Settings.BackColor;
            _settingsTemp.ControlColor = Settings.ControlColor;
            _settingsTemp.IconColor = Settings.IconColor;
            _settingsTemp.SelectedColor = Settings.SelectedColor;
            _settingsTemp.MidiIn = Settings.MidiIn;
            _settingsTemp.MidiOut = Settings.MidiOut;
            _settingsTemp.OscIn = Settings.OscIn;
            _settingsTemp.OscOut = Settings.OscOut;

            pg.SelectedObject = _settingsTemp;
            pg.PropertyValueChanged += (sdr, args) => { _changed = true; };

            // Turn off auto-scaling.
            AutoScaleMode = AutoScaleMode.None;

            // Show the device info.
            // Get dynamic stuff.
            List<string> devText = new()
            {
                // Device info.
                "Your Devices",
                "---------------------------",
                "Midi Inputs:"
            };

            if (MidiIn.NumberOfDevices > 0)
            {
                for (int device = 0; device < MidiIn.NumberOfDevices; device++)
                {
                    devText.Add($"  {MidiIn.DeviceInfo(device).ProductName}");
                }
            }
            else
            {
                devText.Add($"  None");
            }

            devText.Add("Midi Outputs:");
            if (MidiOut.NumberOfDevices > 0)
            {
                for (int device = 0; device < MidiOut.NumberOfDevices; device++)
                {
                    devText.Add($"  {MidiOut.DeviceInfo(device).ProductName}");
                }
            }
            else
            {
                devText.Add($"  None");
            }

            if (AsioOut.GetDriverNames().Length > 0)
            {
                devText.Add("Asio:");
                foreach (string sdev in AsioOut.GetDriverNames())
                {
                    devText.Add($"  {sdev}");
                }
            }
            else
            {
                devText.Add($"  None");
            }

            rtbInfo.Text = string.Join(Environment.NewLine, devText);
        }

        /// <summary>
        /// Editing finished.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Ok_Click(object sender, EventArgs e)
        {
            if (_changed)
            {
                // Copy edited version.
                Settings.AutoCompile = _settingsTemp.AutoCompile;
                Settings.WorkPath = _settingsTemp.WorkPath;
                Settings.CpuMeter = _settingsTemp.CpuMeter;
                Settings.IgnoreWarnings = _settingsTemp.IgnoreWarnings;
                Settings.BackColor = _settingsTemp.BackColor;
                Settings.ControlColor = _settingsTemp.ControlColor;
                Settings.IconColor = _settingsTemp.IconColor;
                Settings.SelectedColor = _settingsTemp.SelectedColor;
                Settings.MidiIn = _settingsTemp.MidiIn;
                Settings.MidiOut = _settingsTemp.MidiOut;
                Settings.OscIn = _settingsTemp.OscIn;
                Settings.OscOut = _settingsTemp.OscOut;

                DialogResult = DialogResult.OK;
            }
            else
            {
                // Nothing to do.
                DialogResult = DialogResult.Cancel;
            }
            Close();
        }

        /// <summary>
        /// Bail out.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Cancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
