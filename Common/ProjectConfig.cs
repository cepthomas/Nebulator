using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.ComponentModel;
using Nebulator.Common;
//using Nebulator.Device;


namespace Nebulator.Common
{
    /// <summary>Defines a controller input.</summary>
    [Serializable]
    public class Controller
    {
        #region Properties
        /// <summary>The associated comm device.</summary>
        [JsonIgnore]
        [Browsable(false)]
        public IInputDevice Device { get; set; } = null;

        /// <summary>The device type for this controller.</summary>
        [JsonIgnore]
        [Browsable(false)]
        public DeviceType DeviceType { get; set; } = DeviceType.None;

        /// <summary>The device name for this controller.</summary>
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
        public string ControllerName { get; set; } = Definitions.UNKNOWN_STRING;
        #endregion

        // /// <summary>The associated numerical (midi) channel to use.</summary>
        // public int ChannelNumber { get; set; } = -1;

        // /// <summary>The numerical controller type. Usually the same as midi.</summary>
        // public int ControllerId { get; set; } = 0;

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            var s = $"Controller: DeviceType:{DeviceType} DeviceName:{DeviceName} ControllerName:{ControllerName}";
            return s;
        }
    }

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
        [JsonIgnore]
        [Browsable(false)]
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


    [Serializable]
    public class ProjectConfig
    {
        /// <summary>Master volume.</summary>
        [DisplayName("xxxx")]
        [Description("xxxx")]
        [Category("xxxx")]
        [Browsable(true)]
        public double MasterVolume { get; set; } = 0.8;

        /// <summary>Tempo.</summary>
        [DisplayName("xxxx")]
        [Description("xxxx")]
        [Category("xxxx")]
        [Browsable(true)]
        public double MasterSpeed { get; set; } = 100.0;

        /// <summary>Active Controllers.</summary>
        [DisplayName("xxxx")]
        [Description("xxxx")]
        [Category("xxxx")]
        [Browsable(true)]
        public List<Controller> Controllers { get; } = new List<Controller>();

        /// <summary>Active Chanels.</summary>
        [DisplayName("xxxx")]
        [Description("xxxx")]
        [Category("xxxx")]
        [Browsable(true)]
        public List<Channel> Channels { get; } = new List<Channel>();

        #region Fields
        /// <summary>The file name.</summary>
        string _fn = Definitions.UNKNOWN_STRING;
        #endregion

        #region Persistence
        /// <summary>Create object from file.</summary>
        public static ProjectConfig Load(string fn)
        {
            ProjectConfig pc = null;

            if (File.Exists(fn))
            {
                string json = File.ReadAllText(fn);
                pc = JsonSerializer.Deserialize<ProjectConfig>(json);

                pc._fn = fn;
            }
            else
            {
                // Doesn't exist, create a new one. TODO1 populate from defaults.
                pc = new ProjectConfig
                {
                    _fn = fn
                };
            }

            // Do post deserialization fixups.
            pc.Channels.ForEach(ch =>
            {
                if (ch.Device.DeviceType != DeviceType.MidiOut && ch.Device.DeviceType != DeviceType.OscOut)
                {
                    throw new Exception($"Invalid device for channel {ch.ChannelName} {ch.ChannelNumber}");
                }

                //TODO1 fixup other props, override from user settings - or in CreateDevices()?
                //    Channel nt = new Channel()
                //    {
                //        Name = name,
                //        DeviceType = device,
                //        ChannelNumber = channelNum,
                //    };

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

                //TODO1 fixup other props, override from user settings - or in CreateControls()?
                //    NController mp = new NController()
                //    {
                //        DeviceType = device,
                //        ChannelNumber = channelNum,
                //        ControllerId = controlId,
                //        BoundVar = bound
                //    };

                pc.Controllers.Add(con);
            });

            return pc;
        }

        /// <summary>Save project to file.</summary>
        public void Save()
        {
            JsonSerializerOptions opts = new() { WriteIndented = true };
            string json = JsonSerializer.Serialize(this, opts);
            File.WriteAllText(_fn, json);
        }
        #endregion
    }
}
