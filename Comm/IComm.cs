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
        string CommName { get; set; }

        /// <summary>What it can do.</summary>
        CommCaps Caps { get; set; }

        /// <summary>Log traffic.</summary>
        bool Monitor { get; set; }
        #endregion

        #region Functions
        /// <summary>Initialize everything. Set valid CommName first!</summary>
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
    public interface ICommInput : IComm
    {
        #region Events
        /// <summary>Reporting a change to listeners.</summary>
        event EventHandler<CommInputEventArgs> CommInputEvent;
        #endregion
    }

    /// <summary>Output specific version.</summary>
    public interface ICommOutput : IComm
    {
        #region Functions
        /// <summary>Comm out processor.</summary>
        /// <param name="step"></param>
        bool Send(Step step);

        /// <summary>Kill one channel.</summary>
        /// <param name="channel">Specific channel or null for all.</param>
        void Kill(int? channel = null);
        #endregion
    }







    ///// <summary>Abstraction layer between low level protocols (e.g. midi, OSC) and Nebulator steps.</summary>
    //public interface IComm : IDisposable
    //{
    //    #region Events
    //    /// <summary>Reporting a change to listeners.</summary>
    //    event EventHandler<CommInputEventArgs> CommInputEvent;

    //    /// <summary>Request for logging service.</summary>
    //    event EventHandler<CommLogEventArgs> CommLogEvent;
    //    #endregion

    //    #region Properties
    //    /// <summary>Comm name.</summary>
    //    string CommName { get; set; }

    //    /// <summary>Which way is traffic.</summary>
    //    CommDirection Direction { get; set; }

    //    /// <summary>What it can do.</summary>
    //    CommCaps Caps { get; set; }

    //    /// <summary>Log traffic.</summary>
    //    bool Monitor { get; set; }
    //    #endregion

    //    #region Functions
    //    /// <summary>Initialize everything. Set valid CommName first!</summary>
    //    bool Init();

    //    /// <summary>Start operation.</summary>
    //    void Start();

    //    /// <summary>Stop operation.</summary>
    //    void Stop();

    //    /// <summary>Background operations such as process any stop notes.</summary>
    //    void Housekeep();

    //    /// <summary>Comm out processor.</summary>
    //    /// <param name="step"></param>
    //    bool Send(Step step);

    //    /// <summary>Kill one channel.</summary>
    //    /// <param name="channel">Specific channel or null for all.</param>
    //    void Kill(int? channel = null);
    //    #endregion
    //}


}
