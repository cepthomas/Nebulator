using System;
using System.Collections.Generic;
using Nebulator.Common;


namespace Nebulator.Protocol
{
    public class ProtocolInputEventArgs : EventArgs
    {
        /// <summary>Received data.</summary>
        public Step Step { get; set; } = null;

        /// <summary>Was it processed.</summary>
        public bool Handled { get; set; } = false;
    }

    public class ProtocolLogEventArgs : EventArgs
    {
        public enum LogCategory { Info, Send, Recv, Error }

        /// <summary>Category.</summary>
        public LogCategory Category { get; set; } = LogCategory.Info;

        /// <summary>Text to log.</summary>
        public string Message { get; set; } = null;
    }

    /// <summary>What it can do. Self explanatory.</summary>
    public class ProtocolCaps
    {
        public int NumChannels { get; set; }
        public int MinVolume { get; set; }
        public int MaxVolume { get; set; }
        public int MinNote { get; set; }
        public int MaxNote { get; set; }
        public int MinControllerValue { get; set; }
        public int MaxControllerValue { get; set; }
        public int MinPitchValue { get; set; }
        public int MaxPitchValue { get; set; }
    }

    /// <summary>Abstraction layer between low level protocols (e.g. midi, OSC) and Nebulator steps.</summary>
    public interface IProtocol
    {
        #region Events
        /// <summary>Reporting a change to listeners.</summary>
        event EventHandler<ProtocolInputEventArgs> ProtocolInputEvent;

        /// <summary>Request for logging service.</summary>
        event EventHandler<ProtocolLogEventArgs> ProtocolLogEvent;
        #endregion

        #region Properties
        /// <summary>What it can do.</summary>
        ProtocolCaps Caps { get; set; }

        /// <summary>All available inputs for UI selection.</summary>
        List<string> ProtocolInputs { get; set; }

        /// <summary>All available outputs for UI selection.</summary>
        List<string> ProtocolOutputs { get; set; }
        #endregion

        #region Functions
        /// <summary>Initialize everything.</summary>
        void Init();

        /// <summary>Start listening for inputs.</summary>
        void Start();

        /// <summary>Stop listening for inputs.</summary>
        void Stop();

        /// <summary>Background operations such as process any stop notes.</summary>
        void Housekeep();

        /// <summary>Protocol out processor.</summary>
        /// <param name="step"></param>
        void Send(Step step);

        /// <summary>Kill one channel.</summary>
        /// <param name="channel">Specific channel or null for all.</param>
        void Kill(int? channel = null);

        /// <summary>Clean up resources.</summary>
        void Dispose();
        #endregion
    }
}
