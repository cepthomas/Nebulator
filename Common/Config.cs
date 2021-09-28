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
    /// <summary>Each nebulator script has an associated configuration file.</summary>
    [Serializable]
    public class Config
    {
        #region Properties
        /// <summary>The file name.</summary>
        [JsonIgnore]
        [Browsable(false)]
        public string FileName { get; private set; } = Definitions.UNKNOWN_STRING;

        /// <summary>Master volume.</summary>
        [Browsable(false)]
        public double MasterVolume { get; set; } = 0.8;

        /// <summary>Tempo.</summary>
        [Browsable(false)]
        public double MasterSpeed { get; set; } = 100.0;

        [DisplayName("Channels")]
        [Description("Active Channels")]
        [Category("xxxx")]
        [Browsable(true)]
        [MaxLength(Channel.NUM_CHANNELS, ErrorMessage = "Channel max is {Channel.NUM_CHANNELS}")]
        public List<Channel> Channels { get; } = new List<Channel>();

        [DisplayName("Controllers")]
        [Description("Active Controllers")]
        [Category("xxxx")]
        [Browsable(true)]
        public List<Controller> Controllers { get; } = new List<Controller>();
        #endregion

        #region Persistence
        /// <summary>Create object from file.</summary>
        public static Config Load(string fn)
        {
            Config pc;

            if (File.Exists(fn))
            {
                string json = File.ReadAllText(fn);
                pc = JsonSerializer.Deserialize<Config>(json);
                pc.FileName = fn;
            }
            else
            {
                // Doesn't exist, create a new one.
                pc = new Config
                {
                    FileName = fn
                };
            }

            // Do post deserialization fixups.
            pc.Channels.ForEach(ch =>
            {
                if (ch.Device.DeviceType != DeviceType.MidiOut && ch.Device.DeviceType != DeviceType.OscOut)
                {
                    throw new Exception($"Invalid device for channel {ch.ChannelName} {ch.ChannelNumber}");
                }

                if (ch.WobbleRange != 0.0)
                {
                    ch.VolWobbler = new Wobbler()
                    {
                        RangeHigh = ch.WobbleRange,
                        RangeLow = ch.WobbleRange
                    };
                }

                pc.Channels.Add(ch);
            });

            pc.Controllers.ForEach(con =>
            {
                if (con.Device.DeviceType != DeviceType.MidiIn && con.Device.DeviceType != DeviceType.OscIn && con.Device.DeviceType != DeviceType.VkeyIn)
                {
                    throw new Exception($"Invalid device for controller {con.Device.DeviceType}");
                }

                pc.Controllers.Add(con);
            });

            return pc;
        }

        /// <summary>Save project to file.</summary>
        public void Save()
        {
            JsonSerializerOptions opts = new() { WriteIndented = true };
            string json = JsonSerializer.Serialize(this, opts);
            File.WriteAllText(FileName, json);
        }
        #endregion
    }
}
