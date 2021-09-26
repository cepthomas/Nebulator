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
    /// <summary>Defines a controller input. Currently just a simple mapping between script and device but could do more.</summary>
    [Serializable]
    public class Controller
    {
        #region Properties
        /// <summary>The associated comm device.</summary>
        [JsonIgnore]
        [Browsable(false)]
        public IInputDevice Device { get; set; } = null;

        [DisplayName("Device Type")]
        [Description("The device type for this controller.")]
        [Category("xxxx")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DeviceType DeviceType { get; set; } = DeviceType.None;

        [DisplayName("Device Name")]
        [Description("The device name for this controller.")]
        [Category("xxxx")]
        [Browsable(true)]
        [TypeConverter(typeof(FixedListTypeConverter))]
        public string DeviceName { get; set; } = Definitions.UNKNOWN_STRING;

        [DisplayName("Controller Name")]
        [Description("The UI name for this channel.")]
        [Category("xxxx")]
        [Browsable(true)]
        public string ControllerNameX { get; set; } = Definitions.UNKNOWN_STRING;
        #endregion

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            var s = $"Controller: DeviceType:{DeviceType} DeviceName:{DeviceName} ControllerName:{ControllerNameX}";
            return s;
        }
    }
}
