using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.ComponentModel;


namespace Nebulator.Common
{
    /// <summary>One channel output.</summary>
    [Serializable]
    public class Channel
    {
        #region Properties
        /// <summary>The associated comm device.</summary>
        [JsonIgnore]
        [Browsable(false)]
        public IOutputDevice Device { get; set; } = null;

        /// <summary>The device type for this channel.</summary>
        [DisplayName("xxxx")]
        [Description("xxxx")]
        [Category("xxxx")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DeviceType DeviceType { get; set; } = DeviceType.None;

        /// <summary>The device name for this channel.</summary>
        [DisplayName("xxxx")]
        [Description("xxxx")]
        [Category("xxxx")]
        [Browsable(true)]
        [TypeConverter(typeof(FixedListTypeConverter))]
        public string DeviceName { get; set; } = Definitions.UNKNOWN_STRING;

        /// <summary>The UI name for this channel.</summary>
        [DisplayName("xxxx")]
        [Description("xxxx")]
        [Category("xxxx")]
        [Browsable(true)]
        public string ChannelName { get; set; } = Definitions.UNKNOWN_STRING;

        /// <summary>The associated numerical (midi) channel to use 1-16.</summary>
        [DisplayName("xxxx")]
        [Description("xxxx")]
        [Category("xxxx")]
        [Browsable(true)]
        public int ChannelNumber { get; set; } = 1;

        //TODO1 Optional patch. See ClipExplorer.
        /// <summary>Current volume.</summary>
        [DisplayName("xxxx")]
        [Description("xxxx")]
        [Category("xxxx")]
        [Browsable(true)]
        public double Volume { get; set; } = 0.8;

        /// <summary>How wobbly.</summary>
        [DisplayName("xxxx")]
        [Description("xxxx")]
        [Category("xxxx")]
        [Browsable(true)]
        public double WobbleRange { get; set; } = 0.0;

        /// <summary>Wobbler for volume (optional).</summary>
        [JsonIgnore]
        [Browsable(false)]
        public Wobbler VolWobbler { get; set; } = null;

        /// <summary>Current state for this channel.</summary>
        [Browsable(false)]
        public ChannelState State { get; set; } = ChannelState.Normal;
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
