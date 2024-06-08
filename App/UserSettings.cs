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
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfUis;
using Ephemera.MidiLib;


namespace Nebulator.App
{
    [Serializable]
    public sealed class UserSettings : SettingsCore
    {
        #region Properties - persisted editable
        [DisplayName("Script Path")]
        [Description("Default location for user scripts.")]
        [Browsable(true)]
        [Editor(typeof(FolderNameEditor), typeof(UITypeEditor))]
        public string ScriptPath { get; set; } = "";

        [DisplayName("Auto Compile")]
        [Description("Compile current file when change detected.")]
        [Browsable(true)]
        public bool AutoCompile { get; set; } = true;

        [DisplayName("Ignore Warnings")]
        [Description("Ignore compiler warnings otherwise treat them as errors.")]
        [Browsable(true)]
        public bool IgnoreWarnings { get; set; } = true;

        [DisplayName("File Log Level")]
        [Description("Log level for file write.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel FileLogLevel { get; set; } = LogLevel.Trace;

        [DisplayName("File Log Level")]
        [Description("Log level for UI notification.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel NotifLogLevel { get; set; } = LogLevel.Debug;

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
