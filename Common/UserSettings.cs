using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms.Design;
using System.Drawing.Design;
using NAudio.Midi;
using NBagOfTricks;
using NBagOfUis;


namespace Nebulator.Common
{
    [Serializable]
    public class UserSettings : Settings
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
        [Editor(typeof(FolderNameEditor), typeof(UITypeEditor))]
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
        #endregion

        #region Properties - internal
        [Browsable(false)]
        public bool Valid { get; set; } = false;

        [Browsable(false)]
        [JsonConverter(typeof(JsonRectangleConverter))]
        public Rectangle KeyboardFormGeometry { get; set; } = new Rectangle(50, 50, 600, 400);

        [Browsable(false)]
        public bool MonitorInput { get; set; } = false;

        [Browsable(false)]
        public bool WordWrap { get; set; } = false;

        [Browsable(false)]
        public bool MonitorOutput { get; set; } = false;
        #endregion
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
