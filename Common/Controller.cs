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
        [DisplayName("xxxx")]
        [Description("xxxx")]
        [Category("xxxx")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
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

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            var s = $"Controller: DeviceType:{DeviceType} DeviceName:{DeviceName} ControllerName:{ControllerName}";
            return s;
        }
    }
}
