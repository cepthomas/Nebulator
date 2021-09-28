using System;
using System.Collections.Generic;


namespace Nebulator.Common
{
    /// <summary>The various devices.</summary>
    public enum DeviceType { None, MidiIn, MidiOut, OscIn, OscOut, VkeyIn }

    /// <summary>Device has received something.</summary>
    public class DeviceInputEventArgs : EventArgs
    {
        /// <summary>Received data.</summary>
        public Step? Step { get; set; }
    }

    /// <summary>Abstraction layer between low level protocols (e.g. midi, OSC) and Nebulator steps.</summary>
    public interface IDevice : IDisposable
    {
        #region Properties
        /// <summary>Device name.</summary>
        string DeviceName { get; }

        /// <summary>Device type.</summary>
        DeviceType DeviceType { get; }
        #endregion

        #region Functions
        /// <summary>
        /// Interfaces don't allow constructors so do this instead.
        /// Corresponds to the definition in the script.
        /// </summary>
        /// <returns></returns>
        bool Init();

        /// <summary>Start operation.</summary>
        void Start();

        /// <summary>Stop operation.</summary>
        void Stop();

        /// <summary>Background operations such as process any stop notes.</summary>
        void Housekeep();
        #endregion
    }

    /// <summary>Input specific version.</summary>
    public interface IInputDevice : IDevice
    {
        #region Events
        /// <summary>Reporting a change to listeners.</summary>
        event EventHandler<DeviceInputEventArgs> DeviceInputEvent;
        #endregion
    }

    /// <summary>Output specific version.</summary>
    public interface IOutputDevice : IDevice
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