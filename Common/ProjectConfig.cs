using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using Nebulator.Common;
using Nebulator.Device;
using System.IO;



namespace Nebulator.Common
{
    ///// <summary>Defines a controller input.</summary>
    //[Serializable]
    //public class Controller
    //{
    //    #region Properties
    //    /// <summary>The associated comm device.</summary>
    //    [JsonIgnore]
    //    public IInputDevice Device { get; set; } = null;

    //    /// <summary>The associated numerical (midi) channel to use.</summary>
    //    public int ChannelNumber { get; set; } = -1;

    //    /// <summary>The numerical controller type. Usually the same as midi.</summary>
    //    public int ControllerId { get; set; } = 0;
    //    #endregion

    //    /// <summary>For viewing pleasure.</summary>
    //    public override string ToString()
    //    {
    //        var s = $"NController: ControllerId:{ControllerId} ChannelNumber:{ChannelNumber}";
    //        //var s = $"NController: ControllerId:{ControllerId} BoundVar:{BoundVar.Name} ChannelNumber:{ChannelNumber}";
    //        return s;
    //    }
    //}

    /// <summary>One channel output.</summary>
    [Serializable]
    public class Channel
    {
        #region Properties
        /// <summary>The associated comm device.</summary>
        [JsonIgnore]
        public IOutputDevice Device { get; set; } = null;

        /// <summary>The UI name for this channel.</summary>
        public string Name { get; set; } = Definitions.UNKNOWN_STRING;

        /// <summary>The associated numerical (midi) channel to use.</summary>
        public int ChannelNumber { get; set; } = -1;

        /// <summary>Current volume.</summary>
        public double Volume { get; set; } = 90;

        /// <summary>How wobbly.</summary>
        public double WobbleRange { get; set; } = 0.0;

        /// <summary>Wobbler for volume (optional).</summary>
        [JsonIgnore]
        public Wobbler VolWobbler { get; set; } = null;

        /// <summary>Current state for this channel.</summary>
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
            return $"NChannel: Name:{Name} ChannelNumber:{ChannelNumber}";
        }
    }


    [Serializable]
    public class ProjectConfig
    {
        public double MasterVolume { get; set; } = 0.8;

        public double MasterSpeed { get; set; } = 100.0;

        public List<IInputDevice> InputDevices { get; } = new List<IInputDevice>();

        public List<Channel> Channels { get; } = new List<Channel>();

        //public List<Controller> Controllers { get; } = new List<Controller>();
        //TODO1 filters for inputs?

        // TODO2 overrides?
        // "MidiInDevice": "",
        // "MidiOutDevice": "VirtualMIDISynth #1",
        // "OscInDevice": "6448",
        // "OscOutDevice": "127.0.0.1:1234",




        ///// <summary>
        ///// Normal factory.
        ///// </summary>
        ///// <param name="name">UI name</param>
        ///// <param name="device">Device type</param>
        ///// <param name="channelNum"></param>
        ///// <param name="volWobble"></param>
        //public NChannel CreateChannel(string name, DeviceType device, int channelNum, double volWobble = 0.0)
        //{
        //    if (device != DeviceType.MidiOut && device != DeviceType.OscOut)
        //    {
        //        throw new Exception($"Invalid device for channel {name} {device}");
        //    }

        //    NChannel nt = new NChannel()
        //    {
        //        Name = name,
        //        DeviceType = device,
        //        ChannelNumber = channelNum,
        //    };

        //    if (volWobble != 0.0)
        //    {
        //        nt.VolWobbler = new Wobbler()
        //        {
        //            RangeHigh = volWobble,
        //            RangeLow = volWobble
        //        };
        //    }

        //    Channels.Add(nt);
        //    return nt;
        //}

        ///// <summary>
        ///// Create a controller input.
        ///// </summary>
        ///// <param name="device">Device type.</param>
        ///// <param name="channelNum">Which channel.</param>
        ///// <param name="controlId">Which</param>
        ///// <param name="bound">NVariable</param>
        //public void CreateController(DeviceType device, int channelNum, int controlId)//, NVariable bound)
        //{
        //    if (device != DeviceType.MidiIn && device != DeviceType.OscIn && device != DeviceType.VkeyIn)
        //    {
        //        throw new Exception($"Invalid device for controller {device}");
        //    }

        //    //if (bound is null)
        //    //{
        //    //    throw new Exception($"Invalid NVariable for controller {device}");
        //    //}

        //    NController mp = new NController()
        //    {
        //        DeviceType = device,
        //        ChannelNumber = channelNum,
        //        ControllerId = controlId,
        //        BoundVar = bound
        //    };
        //    Controllers.Add(mp);
        //}

        #region Fields
        /// <summary>The file name.</summary>
        string _fn = Definitions.UNKNOWN_STRING;
        #endregion

        #region Persistence
        /// <summary>Save object to file.</summary>
        public void Save()
        {
            JsonSerializerOptions opts = new() { WriteIndented = true };
            string json = JsonSerializer.Serialize(this, opts);
            File.WriteAllText(_fn, json);
        }

        /// <summary>Create object from file.</summary>
        public static ProjectConfig Load(string fn)
        {
            ProjectConfig pc = null;

            if (File.Exists(fn))
            {
                string json = File.ReadAllText(fn);
                pc = JsonSerializer.Deserialize<ProjectConfig>(json);

                // Clean up any bad file names.
               // pc.RecentFiles.RemoveAll(f => !File.Exists(f));

                pc._fn = fn;
            }
            else
            {
                // Doesn't exist, create a new one.
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
                    throw new Exception($"Invalid device for channel {ch.Name} {ch.ChannelNumber}");
                }

                if (ch.WobbleRange != 0.0)
                {
                    ch.VolWobbler = new Wobbler()
                    {
                        RangeHigh = ch.WobbleRange,
                        RangeLow = ch.WobbleRange
                    };
                }
            });


            //pc.Controllers.ForEach(con =>
            //{
            //    if (con.Device.DeviceType != DeviceType.MidiIn && con.Device.DeviceType != DeviceType.OscIn && con.Device.DeviceType != DeviceType.VkeyIn)
            //    {
            //        throw new Exception($"Invalid device for controller {con.Device.DeviceType}");
            //    }
            //});

            return pc;
        }
        #endregion

    }
}
