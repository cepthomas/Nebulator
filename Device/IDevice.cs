using System;
using System.Collections.Generic;
using Nebulator.Common;


namespace Nebulator.Device
{
    /// <summary>The various devices.</summary>
    public enum DeviceType { None, MidiIn, MidiOut, OscIn, OscOut, VkeyIn }

    /// <summary>Abstraction layer between low level protocols (e.g. midi, OSC) and Nebulator steps.</summary>
    public interface IDevice : IDisposable
    {
        #region Events
        /// <summary>Request for logging service.</summary>
        event EventHandler<DeviceLogEventArgs> DeviceLogEvent;
        #endregion

        #region Properties
        /// <summary>Device name.</summary>
        string DeviceName { get; }
        #endregion

        #region Functions
        /// <summary>
        /// Interfaces don't allow constructors so do this instead.
        /// Corresponds to the definition in the script.
        /// </summary>
        /// <param name="device">Specific device.</param>
        /// <returns></returns>
        bool Init(DeviceType device);

        /// <summary>Start operation.</summary>
        void Start();

        /// <summary>Stop operation.</summary>
        void Stop();

        /// <summary>Background operations such as process any stop notes.</summary>
        void Housekeep();
        #endregion
    }

    /// <summary>Input specific version. Slight deviation from naming convention to fit our model.</summary>
    public interface NInput : IDevice
    {
        #region Events
        /// <summary>Reporting a change to listeners.</summary>
        event EventHandler<DeviceInputEventArgs> DeviceInputEvent;
        #endregion
    }

    /// <summary>Output specific version. Slight deviation from naming convention to fit our model.</summary>
    public interface NOutput : IDevice
    {
        #region Functions
        /// <summary>Device out processor.</summary>
        /// <param name="step"></param>
        bool Send(Step step);

        /// <summary>Kill channel(s).</summary>
        /// <param name="channel">Specific channel or null for all.</param>
        void Kill(int? channel = null);
        #endregion
    }
}