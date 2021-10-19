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
        /// <summary>Current global user settings.</summary>
        public static UserSettings TheSettings { get; set; } = new();

        #region Properties - persisted editable
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
        [TypeConverter(typeof(MidiDeviceTypeConverter))]
        public string MidiIn { get; set; } = "None";

        [DisplayName("Midi Output")]
        [Description("Valid device if sending midi output.")]
        [Category("Devices")]
        [Browsable(true)]
        [TypeConverter(typeof(MidiDeviceTypeConverter))]
        public string MidiOut { get; set; } = "None";

        [DisplayName("OSC Input")]
        [Description("Valid port number if handling OSC input.")]
        [Category("Devices")]
        [Browsable(true)]
        public string OscIn { get; set; } = "None";

        [DisplayName("OSC Output")]
        [Description("Valid url:port if sending OSC output.")]
        [Category("Devices")]
        [Browsable(true)]
        public string OscOut { get; set; } = "None";

        [DisplayName("Work Path")]
        [Description("Where you keep your neb files.")]
        [Category("Functionality")]
        [Browsable(true)]
        [Editor(typeof(System.Windows.Forms.Design.FolderNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
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

        [DisplayName("CPU Meter")] //TODO useful? improve?
        [Description("Show a CPU usage meter. Note that this slows start up a bit.")]
        [Category("Functionality")]
        [Browsable(true)]
        public bool CpuMeter { get; set; } = true;
        #endregion

        #region Properties - internal
        [Browsable(false)]
        public bool Valid { get; set; } = false;

        [Browsable(false)]
        public FormInfo MainFormInfo { get; set; } = new FormInfo();

        [Browsable(false)]
        public FormInfo KeyboardInfo { get; set; } = new FormInfo() { Height = 100, Width = 1000 };

        [Browsable(false)]
        public bool MonitorInput { get; set; } = false;

        [Browsable(false)]
        public bool WordWrap { get; set; } = false;

        [Browsable(false)]
        public bool MonitorOutput { get; set; } = false;

        [Browsable(false)]
        public bool Keyboard { get; set; } = true;

        [Browsable(false)]
        public List<string> RecentFiles { get; set; } = new List<string>();
        #endregion

        #region Fields
        /// <summary>The file name.</summary>
        string _fn = Definitions.UNKNOWN_STRING;
        #endregion

        #region Persistence
        /// <summary>Create object from file.</summary>
        public static UserSettings Load(string appDir)
        {
            UserSettings set = new();
            string fn = Path.Combine(appDir, "settings.json");

            if (File.Exists(fn))
            {
                string json = File.ReadAllText(fn);
                var jobj = JsonSerializer.Deserialize<UserSettings>(json);
                if (jobj is not null)
                {
                    set = jobj;
                    set._fn = fn;
                    set.Valid = true;
                }
            }
            else
            {
                // Doesn't exist, create a new one.
                set = new UserSettings()
                {
                    _fn = fn,
                    Valid = true
                };
            }

            return set;
        }

        /// <summary>Save object to file.</summary>
        public void Save()
        {
            if(Valid)
            {
                JsonSerializerOptions opts = new() { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, opts);
                File.WriteAllText(_fn, json);
            }
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


    /// <summary>Converter for selecting property value from known lists.
    public class MidiDeviceTypeConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { return true; }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return true; }

        // Get the specific list based on the property name.
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<string>? rec = null;

            switch (context.PropertyDescriptor.Name)
            {
                case "MidiIn":
                    rec = new() { "" };
                    for (int devindex = 0; devindex < MidiIn.NumberOfDevices; devindex++)
                    {
                        rec.Add(MidiIn.DeviceInfo(devindex).ProductName);
                    }
                    break;

                case "MidiOut":
                    rec = new() { "" };
                    for (int devindex = 0; devindex < MidiOut.NumberOfDevices; devindex++)
                    {
                        rec.Add(MidiOut.DeviceInfo(devindex).ProductName);
                    }
                    break;

                default:
                    System.Windows.Forms.MessageBox.Show($"This should never happen: {context.PropertyDescriptor.Name}");
                    break;
            }

            return new StandardValuesCollection(rec);
        }
    }

}
