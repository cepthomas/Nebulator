using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;


namespace Nebulator.Common
{
    /// <summary>One channel output.</summary>
    [Serializable]
    public class Channel
    {
        /// <summary>Same as midi.</summary>
        public const int NUM_CHANNELS = 16;

        #region Properties
        [DisplayName("Channel Name")]
        [Description("The UI name for this channel.")]
        [Browsable(true)]
        public string ChannelName { get; set; } = Definitions.UNKNOWN_STRING;

        [DisplayName("Channel Number")]
        [Description("The associated numerical (midi) channel to use")]
        [Browsable(true)]
        [Range(1, NUM_CHANNELS, ErrorMessage = "Channel must be 1 to {NUM_CHANNELS}")]
        public int ChannelNumber { get; set; } = 1;

        [DisplayName("Patch")]
        [Description("Optional patch.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Patch Patch { get; set; } = Patch.AcousticGrandPiano;

        [DisplayName("Device Type")]
        [Description("The device type for this channel.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DeviceType DeviceType { get; set; } = DeviceType.None;

        [DisplayName("Device Name")]
        [Description("The system device name for this channel.")]
        [Browsable(true)]
        [TypeConverter(typeof(FixedListTypeConverter))]
        public string DeviceName { get; set; } = Definitions.UNKNOWN_STRING;

        [DisplayName("Volume Wobble Range")]
        [Description("How wobbly. 0 to disable.")]
        [Browsable(true)]
        public double VolumeWobbleRange { get; set; } = 0.0;

        /// <summary>The associated comm device.</summary>
        [JsonIgnore]
        [Browsable(false)]
        public IOutputDevice? Device { get; set; }

        /// <summary>Current volume.</summary>
        [Browsable(false)]
        public double Volume { get; set; } = 0.8;

        /// <summary>Current state for this channel.</summary>
        [Browsable(false)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ChannelState State { get; set; } = ChannelState.Normal;

        /// <summary>Wobbler for volume (optional).</summary>
        [JsonIgnore]
        [Browsable(false)]
        public Wobbler? VolWobbler { get; set; } = null;
        #endregion

        /// <summary>Get the next volume.</summary>
        /// <param name="def"></param>
        /// <returns></returns>
        public double NextVol(double def)
        {
            return VolWobbler is null ? def : VolWobbler.Next(def);
        }

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            var s = $"Controller: DeviceType:{DeviceType} DeviceName:{DeviceName} ChannelName:{ChannelName}";
            return s;
        }
    }
}
