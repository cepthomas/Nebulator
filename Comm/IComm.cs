using System;
using System.Collections.Generic;
using Nebulator.Common;
using Nebulator.Comm;


namespace Nebulator.Comm
{
    /// <summary>Abstraction layer between low level protocols (e.g. midi, OSC) and Nebulator steps.</summary>
    public interface IComm : IDisposable
    {
        #region Events
        /// <summary>Request for logging service.</summary>
        event EventHandler<CommLogEventArgs> CommLogEvent;
        #endregion

        #region Properties
        /// <summary>Comm name.</summary>
        string CommName { get; }

        /// <summary>What it can do. Set by implementation.</summary>
        CommCaps Caps { get; }

        /// <summary>It's alive.</summary>
        bool Inited { get; }
        #endregion

        #region Functions
        /// <summary>
        /// Interfaces don't allow constructors so do this instead.
        /// Corresponds to the definition in the script.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        bool Construct(string name);

        /// <summary>Actually create the comm device.</summary>
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
    public interface NInput : IComm
    {
        #region Events
        /// <summary>Reporting a change to listeners.</summary>
        event EventHandler<CommInputEventArgs> CommInputEvent;
        #endregion
    }

    /// <summary>Output specific version.</summary>
    public interface NOutput : IComm
    {
        #region Functions
        /// <summary>Comm out processor.</summary>
        /// <param name="step"></param>
        bool Send(Step step);

        /// <summary>Kill channel(s).</summary>
        /// <param name="channel">Specific channel or null for all.</param>
        void Kill(int? channel = null);
        #endregion
    }
}