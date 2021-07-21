using System;
using System.Collections.Generic;
using Nebulator.Common;


namespace Nebulator.Device
{
    /// <summary>Category types.</summary>
    public enum DeviceLogCategory { Info, Send, Recv, Error }

    /// <summary>Device has received something.</summary>
    public class DeviceInputEventArgs : EventArgs
    {
        /// <summary>Received data.</summary>
        public Step Step { get; set; } = null;
    }

    /// <summary>Device wants to say something.</summary>
    public class DeviceLogEventArgs : EventArgs
    {
        /// <summary>Category.</summary>
        public DeviceLogCategory DeviceLogCategory { get; set; } = DeviceLogCategory.Info;

        /// <summary>Text to log.</summary>
        public string Message { get; set; } = null;
    }
}
