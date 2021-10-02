using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using NAudio.Midi;
using NBagOfTricks;


namespace Nebulator.Common
{
    [Serializable]
    public class UserSettings
    {
        #region Persisted editable properties
        [DisplayName("Icon Color")]
        [Description("The color used for button icons.")]
        [Category("Cosmetics")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color IconColor { get; set; } = Color.Purple;

        [DisplayName("Control Color")]
        [Description("The color used for active control surfaces.")]
        [Category("Cosmetics")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color ControlColor { get; set; } = Color.Yellow;

        [DisplayName("Selected Color")]
        [Description("The color used for selected controls.")]
        [Category("Cosmetics")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color SelectedColor { get; set; } = Color.Violet;

        [DisplayName("Background Color")]
        [Description("The color used for overall background.")]
        [Category("Cosmetics")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color BackColor { get; set; } = Color.AliceBlue;

        [DisplayName("Midi Input")]
        [Description("Valid device if handling midi input.")]
        [Category("Devices")]
        [Browsable(true)]
        [TypeConverter(typeof(FixedListTypeConverter))]
        public string MidiInDevice { get; set; } = "";

        [DisplayName("Midi Output")]
        [Description("Valid device if sending midi output.")]
        [Category("Devices")]
        [Browsable(true)]
        [TypeConverter(typeof(FixedListTypeConverter))]
        public string MidiOutDevice { get; set; } = "";

        [DisplayName("OSC Input")]
        [Description("Valid port number if handling OSC input.")]
        [Category("Devices")]
        [Browsable(true)]
        [TypeConverter(typeof(FixedListTypeConverter))]
        public string OscInDevice { get; set; } = "6448";

        [DisplayName("OSC Output")]
        [Description("Valid url:port if sending OSC output.")]
        [Category("Devices")]
        [Browsable(true)]
        [TypeConverter(typeof(FixedListTypeConverter))]
        public string OscOutDevice { get; set; } = "127.0.0.1:1234";

        //[DisplayName("Virtual Keyboard")]
        //[Description("Show the keyboard.")]
        //[Category("Devices")]
        //[Browsable(true)]
        //public bool VirtualKeyboard { get; set; } = true; TODO2

        [DisplayName("Work Path")]
        [Description("Where you keep your neb files.")]
        [Category("Functionality")]
        [Browsable(true)]
        public string WorkPath { get; set; } = "";

        [DisplayName("Auto Compile")]
        [Description("Compile current file when change detected.")]
        [Category("Functionality")]
        [Browsable(true)]
        public bool AutoCompile { get; set; } = true;

        [DisplayName("Ignore Warnings")]
        [Description("Ignore warnings otherwise treat them as errors.")]
        [Category("Functionality")]
        [Browsable(true)]
        public bool IgnoreWarnings { get; set; } = true;

        [DisplayName("CPU Meter")] //TODO2 useful? improve?
        [Description("Show a CPU usage meter. Note that this slows start up a bit.")]
        [Category("Functionality")]
        [Browsable(true)]
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
        public bool WordWrap { get; set; } = false;

        [Browsable(false)]
        public bool MonitorOutput { get; set; } = false;

        [Browsable(false)]
        public List<string> RecentFiles { get; set; } = new List<string>();
        #endregion

        #region Fields
        /// <summary>The file name.</summary>
        string _fn = Definitions.UNKNOWN_STRING;
        #endregion

        /// <summary>Current global user settings.</summary>
        public static UserSettings TheSettings { get; set; } = new UserSettings();

        #region Persistence
        /// <summary>Create object from file.</summary>
        public static UserSettings? Load(string appDir)
        {
            UserSettings? set;
            string fn = Path.Combine(appDir, "settings.json");

            if (File.Exists(fn))
            {
                string json = File.ReadAllText(fn);
                var jobj = JsonSerializer.Deserialize<UserSettings>(json);
                if (jobj is not null)
                {
                    set = jobj;
                    set._fn = fn;
                }
                else
                {
                    throw new Exception($"Invalid user settings file: {fn}");
                }
            }
            else
            {
                // Doesn't exist, create a new one.
                set = new UserSettings()
                {
                    _fn = fn
                };
            }

            return set;
        }

        /// <summary>Save object to file.</summary>
        public void Save()
        {
            JsonSerializerOptions opts = new() { WriteIndented = true };
            string json = JsonSerializer.Serialize(this, opts);
            File.WriteAllText(_fn, json);
        }
        #endregion
    }

    /// <summary>General purpose container for persistence.</summary>
    [Serializable]
    public class FormInfo
    {
        public int X { get; set; } = 50;
        public int Y { get; set; } = 50;
        public int Width { get; set; } = 1000;
        public int Height { get; set; } = 700;
    }
}
