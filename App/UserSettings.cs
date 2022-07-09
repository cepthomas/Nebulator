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
using MidiLib;


namespace Nebulator.App
{
    [Serializable]
    public class UserSettings : Settings
    {
        /// <summary>Current global user settings.</summary>
        public static UserSettings TheSettings { get; set; } = new();

        #region Properties - persisted editable
        [DisplayName("Icon Color")]
        [Description("The color used for button icons.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color IconColor { get; set; } = Color.Purple;

        [DisplayName("Control Color")]
        [Description("The color used for active control surfaces.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color ControlColor { get; set; } = Color.Yellow;

        [DisplayName("Selected Color")]
        [Description("The color used for selected controls.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color SelectedColor { get; set; } = Color.Violet;

        [DisplayName("Background Color")]
        [Description("The color used for overall background.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color BackColor { get; set; } = Color.AliceBlue;

        [DisplayName("Auto Compile")]
        [Description("Compile current file when change detected.")]
        [Browsable(true)]
        public bool AutoCompile { get; set; } = true;

        [DisplayName("Ignore Warnings")]
        [Description("Ignore compiler warnings otherwise treat them as errors.")]
        [Browsable(true)]
        public bool IgnoreWarnings { get; set; } = true;

        [DisplayName("Midi Settings")]
        [Description("Edit midi settings.")]
        [Browsable(true)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public MidiSettings MidiSettings { get; set; } = new();
        #endregion

        #region Properties - internal
        [Browsable(false)]
        public bool Valid { get; set; } = false;

        [Browsable(false)]
        public bool MonitorInput { get; set; } = false;

        [Browsable(false)]
        public bool WordWrap { get; set; } = false;

        [Browsable(false)]
        public bool MonitorOutput { get; set; } = false;
        #endregion
    }
}
