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
        public Font EditorFont { get; set; } = new Font("Consolas", 10);

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

        [DisplayName("CPU Meter"), Description("Show a CPU usage meter. Note that this slows start up a bit."), Browsable(true)]
        public bool CpuMeter { get; set; } = true;
        #endregion

        #region Persisted non-editable properties
        [Browsable(false)]
        public FormInfo MainFormInfo { get; set; } = new FormInfo();

        [Browsable(false)]
        public FormInfo VirtualKeyboardInfo { get; set; } = new FormInfo() { Height = 100, Width = 1000 };

        [Browsable(false)]
        public bool MonitorInput { get; set; } = false;

        [Browsable(false)]
        public bool MonitorOutput { get; set; } = false;

        [Browsable(false)]
        public List<string> RecentFiles { get; set; } = new List<string>();
        #endregion

        #region Classes
        /// <summary>
        /// General purpose container for persistence.
        /// </summary>
        [Serializable]
        public class FormInfo
        {
            public int X { get; set; } = 50;
            public int Y { get; set; } = 50;
            public int Width { get; set; } = 1000;
            public int Height { get; set; } = 700;

            public void FromForm(Form f)
            {
                X = f.Location.X;
                Y = f.Location.Y;
                Width = f.Width;
                Height = f.Height;
            }
        }
        #endregion

        #region Fields
        /// <summary>The file name.</summary>
        string _fn = Utils.UNKNOWN_STRING;
        #endregion

        /// <summary>Current global user settings.</summary>
        public static UserSettings TheSettings { get; set; } = new UserSettings();

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

            if(File.Exists(fn))
            {
                string json = File.ReadAllText(fn);
                TheSettings = JsonConvert.DeserializeObject<UserSettings>(json);

                // Clean up any bad file names.
                TheSettings.RecentFiles.RemoveAll(f => !File.Exists(f));

                TheSettings._fn = fn;
            }
            else
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
}
