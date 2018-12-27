using System;
using System.Collections.Generic;
using Nebulator.Common;


namespace Nebulator.Device
{
    /// <summary>Device type.</summary>
    public enum DeviceType { Midi, OSC, ASIO, VirtualKeyboard }

    public class DeviceInputEventArgs : EventArgs
    {
        /// <summary>Received data.</summary>
        public Step Step { get; set; } = null;
    }

    public class DeviceLogEventArgs : EventArgs
    {
        /// <summary>Category types.</summary>
        public enum LogCategory { Info, Send, Recv, Error }

        /// <summary>Category.</summary>
        public LogCategory Category { get; set; } = LogCategory.Info;

        /// <summary>Text to log.</summary>
        public string Message { get; set; } = null;
    }
}
