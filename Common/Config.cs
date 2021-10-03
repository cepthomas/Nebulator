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

        /// <summary>Is this config ok?</summary>
        [JsonIgnore]
        [Browsable(false)]
        public bool Valid { get; set; } = true;

        /// <summary>Master volume.</summary>
        [Browsable(false)]
        public double MasterVolume { get; set; } = 0.8;

        /// <summary>Tempo.</summary>
        [Browsable(false)]
        public double MasterSpeed { get; set; } = 100.0;

        [DisplayName("Channels")]
        [Description("Active Channels")]
        [Browsable(true)]
        [MaxLength(Channel.NUM_CHANNELS, ErrorMessage = "Channel between 1 and 16")]
        public List<Channel> Channels { get; set; } = new List<Channel>();

        //[DisplayName("Controllers")]
        //[Description("Active Controllers")]
        //[Browsable(true)]
        //public List<Controller> Controllers { get; set; } = new List<Controller>();
        #endregion

        #region Persistence
        /// <summary>Create object from file.</summary>
        public static Config? Load(string fn)
        {
            Config? pc;

            if (File.Exists(fn))
            {
                JsonSerializerOptions opts = new() { AllowTrailingCommas = true };
                string json = File.ReadAllText(fn);
                pc = JsonSerializer.Deserialize<Config>(json, opts);
                if(pc is not null)
                {
                    pc.FileName = fn;

                    // Do post deserialization fixups.
                    pc.Channels.ForEach(ch =>
                    {
                        if (ch.VolumeWobbleRange != 0.0)
                        {
                            ch.VolWobbler = new Wobbler()
                            {
                                RangeHigh = ch.VolumeWobbleRange,
                                RangeLow = ch.VolumeWobbleRange
                            };
                        }
                    });
                }
                //else
                //{
                //    throw new Exception($"Invalid config file: {fn}");
                //}
            }
            else
            {
                // Doesn't exist, create a new one.
                pc = new Config
                {
                    FileName = fn
                };
            }

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

        /// <summary>Create object from file.</summary>
        public static void MakeFake(string fn)
        {
            Config pc = new()
            {
                FileName = fn,
                MasterVolume = 0.1234,
                MasterSpeed = 67.89
            };

            pc.Channels.Add(new()
            {
                DeviceType = DeviceType.MidiOut,
                //DeviceName = "DevOut1",
                ChannelName = "keys",
                ChannelNumber = 1,
                Patch = Patch.AcousticGrandPiano,
                VolumeWobbleRange = 0.0,
                Volume = 0.8,
                State = ChannelState.Normal
            });

            pc.Channels.Add(new()
            {
                DeviceType = DeviceType.OscOut,
                //DeviceName = "DevOut2",
                ChannelName = "bass",
                ChannelNumber = 2,
                Patch = Patch.AcousticBass,
                VolumeWobbleRange = 0.2,
                Volume = 0.75,
                State = ChannelState.Normal
            });
            pc.Channels.Add(new()
            {
                DeviceType = DeviceType.MidiOut,
                //DeviceName = "DevOut3",
                ChannelName = "synth",
                ChannelNumber = 3,
                Patch = Patch.Lead2Sawtooth,
                VolumeWobbleRange = 0.3,
                Volume = 0.6,
                State = ChannelState.Normal
            });
            pc.Channels.Add(new()
            {
                DeviceType = DeviceType.MidiOut,
                //DeviceName = "DevOut4",
                ChannelName = "drums",
                ChannelNumber = 10,
                //Patch = Patch.,
                VolumeWobbleRange = 0.4,
                Volume = 0.4,
                State = ChannelState.Solo
            });

            //pc.Controllers.Add(new()
            //{
            //    DeviceType = DeviceType.MidiIn,
            //    //DeviceName = "DevIn1",
            //    ControllerName = "Ctrl1"
            //});

            //pc.Controllers.Add(new()
            //{
            //    DeviceType = DeviceType.MidiIn,
            //    //DeviceName = "DevIn2",
            //    ControllerName = "Ctrl2"
            //});

            //pc.Controllers.Add(new()
            //{
            //    DeviceType = DeviceType.OscIn,
            //    //DeviceName = "DevIn3",
            //    ControllerName = "Ctrl3"
            //});

            pc.Save();
        }
    }
}
