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
        [DisplayName("Controller Name")]
        [Description("The UI name for this channel.")]
        [Browsable(true)]
        public string ControllerName { get; set; } = Definitions.UNKNOWN_STRING;

        [DisplayName("Device Type")]
        [Description("The device type for this controller.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DeviceType DeviceType { get; set; } = DeviceType.None;

        [DisplayName("Device Name")]
        [Description("The device name for this controller.")]
        [Browsable(true)]
        [TypeConverter(typeof(FixedListTypeConverter))]
        public string DeviceName { get; set; } = Definitions.UNKNOWN_STRING;

        /// <summary>The associated comm device.</summary>
        [JsonIgnore]
        [Browsable(false)]
        public IInputDevice? Device { get; set; }
        #endregion

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            var s = $"Controller: DeviceType:{DeviceType} DeviceName:{DeviceName} ControllerName:{ControllerName}";
            return s;
        }
    }
}
