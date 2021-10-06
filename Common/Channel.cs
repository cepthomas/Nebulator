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
    //[Serializable]
    public class Channel
    {
        /// <summary>Same as midi.</summary>
        public const int NUM_CHANNELS = 16;

        #region Properties - editable
        /// <summary>UI label and script reference.</summary>
        public string ChannelName { get; set; } = Definitions.UNKNOWN_STRING;

        /// <summary>The associated numerical (midi) channel to use</summary>
        public int ChannelNumber { get; set; } = 1;

        /// <summary>Optional patch to send at startup.</summary>
        public Patch Patch { get; set; } = Patch.AcousticGrandPiano;

        /// <summary>The device type for this channel.</summary>
        public DeviceType DeviceType { get; set; } = DeviceType.None;

        /// <summary>How wobbly. 0 to disable.</summary>
        public double VolumeWobbleRange { get; set; } = 0.0;
        #endregion

        #region Properties - internal
        /// <summary>The associated device object.</summary>
        [Browsable(false)]
        [JsonIgnore]
        public IOutputDevice? Device { get; set; } = null;

        /// <summary>Current volume.</summary>
        [Browsable(false)]
        public double Volume { get; set; } = 0.8;

        /// <summary>Current state for this channel.</summary>
        [Browsable(false)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ChannelState State { get; set; } = ChannelState.Normal;

        /// <summary>Wobbler for volume (optional).</summary>
        [Browsable(false)]
        [JsonIgnore]
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
            var s = $"Channel: DeviceType:{DeviceType} ChannelName:{ChannelName}";
            //var s = $"Channel: DeviceType:{DeviceType} DeviceName:{DeviceName} ChannelName:{ChannelName}";
            return s;
        }
    }
}
