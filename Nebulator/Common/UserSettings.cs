using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Drawing.Design;
using Newtonsoft.Json;
using Nebulator.UI;


namespace Nebulator.Common
{
    [Serializable]
    public class UserSettings
    {
        #region Persisted editable properties
        [DisplayName("Editor Font"), Description("The font to use for editors etc."), Browsable(true)]
        public Font EditorFont { get; set; } = new Font("Consolas", 9);

        [DisplayName("Control Font"), Description("The font to use for controls."), Browsable(true)]
        public Font ControlFont { get; set; } = new Font("Microsoft Sans Serif", 9, FontStyle.Bold);

        [DisplayName("Icon Color"), Description("The color used for icons."), Browsable(true)]
        public Color IconColor { get; set; } = Color.Purple;

        [DisplayName("Control Color"), Description("The color used for styling control surfaces."), Browsable(true)]
        public Color ControlColor { get; set; } = Color.Yellow;

        [DisplayName("Selected Color"), Description("The color used for selections."), Browsable(true)]
        public Color SelectedColor { get; set; } = Color.Violet;

        [DisplayName("Background Color"), Description("The color used for overall background."), Browsable(true)]
        public Color BackColor { get; set; } = Color.AliceBlue;

        [DisplayName("Loop Color"), Description("The color used for loop block display."), Browsable(true)]
        public Color LoopColor { get; set; } = Color.Salmon;

        [DisplayName("Midi Input"), Description("Your choice of midi input."), Browsable(true)]
        [Editor(typeof(MidiPortEditor), typeof(UITypeEditor))]
        public string MidiIn { get; set; } = Globals.UNKNOWN_STRING; // FUTURE support more than one midi port.

        [DisplayName("Midi Output"), Description("Your choice of midi output.")]
        [Editor(typeof(MidiPortEditor), typeof(UITypeEditor)), Browsable(true)]
        public string MidiOut { get; set; } = Globals.UNKNOWN_STRING;

        [DisplayName("Chords"), Description("Your custom chords in the form of name: 1 2 b5 9.")]
        [Editor(typeof(StringListEditor), typeof(UITypeEditor))]
        public List<string> Chords { get; set; } = new List<string>();

        [DisplayName("Timer Debug"), Description("Turn on some metrics gathering.")]
        public bool TimerStats { get; set; } = false;
        #endregion

        #region Persisted non-editable properties
        [Browsable(false)]
        public FormInfo MainFormInfo { get; set; } = new FormInfo();

        [Browsable(false)]
        public FormInfo PianoFormInfo { get; set; } = new FormInfo() { Height = 100, Width = 1000, Visible = true };

        [Browsable(false)]
        public bool MidiMonitorIn { get; set; } = false;

        [Browsable(false)]
        public bool MidiMonitorOut { get; set; } = false;

        [Browsable(false)]
        public bool LeversOn { get; set; } = false;

        [Browsable(false)]
        public List<string> RecentFiles { get; set; } = new List<string>();

        [Browsable(false)]
        public int ControlSplitterPos { get; set; } = 800;
        #endregion

        /// <summary>The file name.</summary>
        string _fn = Globals.UNKNOWN_STRING;

        /// <summary>Default constructor.</summary>
        public UserSettings()
        {
        }

        #region Persistence
        /// <summary>Save object to file.</summary>
        public void Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(_fn, json);
        }

        /// <summary>Create object from file.</summary>
        public static UserSettings Load(string appDir)
        {
            UserSettings us = null;
            string fn = Path.Combine(appDir, "settings.json");

            try
            {
                string json = File.ReadAllText(fn);
                us = JsonConvert.DeserializeObject<UserSettings>(json);
                us._fn = fn;
            }
            catch (Exception)
            {
                // Doesn't exist, create a new one.
                us = new UserSettings
                {
                    _fn = fn
                };
            }

            return us;
        }
        #endregion
    }

    [Serializable]
    public class FormInfo
    {
        public bool Visible { get; set; } = false;
        public int X { get; set; } = 50;
        public int Y { get; set; } = 50;
        public int Width { get; set; } = 1000;
        public int Height { get; set; } = 700;

        public void FromForm(Form f)
        {
            Visible = f.Visible;
            X = f.Location.X;
            Y = f.Location.Y;
            Width = f.Width;
            Height = f.Height;
        }
    }
}
