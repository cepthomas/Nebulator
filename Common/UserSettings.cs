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

        [DisplayName("Word Wrap"), Description("Set UI preference."), Browsable(true)]
        public bool WordWrap { get; set; } = false;

        [DisplayName("Lock UI"), Description("Forces UI to always topmost."), Browsable(true)]
        public bool LockUi { get; set; } = false;

        [DisplayName("Midi Input"), Description("Your choice of midi input."), Browsable(true)]
        [Editor(typeof(ListSelector), typeof(UITypeEditor))]
        public string MidiIn { get; set; } = Utils.UNKNOWN_STRING;

        [DisplayName("Midi Output"), Description("Your choice of midi output."), Browsable(true)]
        [Editor(typeof(ListSelector), typeof(UITypeEditor))]
        public string MidiOut { get; set; } = Utils.UNKNOWN_STRING;
        #endregion

        #region Persisted non-editable properties
        [Browsable(false)]
        public FormInfo MainFormInfo { get; set; } = new FormInfo();

        [Browsable(false)]
        public FormInfo SurfaceFormInfo { get; set; } = new FormInfo();

        [Browsable(false)]
        public FormInfo PianoFormInfo { get; set; } = new FormInfo() { Height = 100, Width = 1000, Visible = true };

        [Browsable(false)]
        public bool MidiMonitorIn { get; set; } = false;

        [Browsable(false)]
        public bool MidiMonitorOut { get; set; } = false;

        [Browsable(false)]
        public List<string> RecentFiles { get; set; } = new List<string>();
        #endregion

        #region Fields
        /// <summary>The file name.</summary>
        string _fn = Utils.UNKNOWN_STRING;
        #endregion

        /// <summary>Current global user settings.</summary>
        public static UserSettings TheSettings { get; set; } = new UserSettings();

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
        public static void Load(string appDir)
        {
            TheSettings = null;
            string fn = Path.Combine(appDir, "settings.json");

            try
            {
                string json = File.ReadAllText(fn);
                TheSettings = JsonConvert.DeserializeObject<UserSettings>(json);

                // Clean up bad file names.
                TheSettings.RecentFiles.RemoveAll(f => !File.Exists(f));

                TheSettings._fn = fn;
            }
            catch (Exception)
            {
                // Doesn't exist, create a new one.
                TheSettings = new UserSettings
                {
                    _fn = fn
                };
            }
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
