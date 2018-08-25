using System;
using System.Collections.Generic;
using Nebulator.Common;


namespace Nebulator.Comm
{
    public class CommInputEventArgs : EventArgs
    {
        /// <summary>Received data.</summary>
        public Step Step { get; set; } = null;
    }

    public class CommLogEventArgs : EventArgs
    {
        /// <summary>Category types.</summary>
        public enum LogCategory { Info, Send, Recv, Error }

        /// <summary>Category.</summary>
        public LogCategory Category { get; set; } = LogCategory.Info;

        /// <summary>Text to log.</summary>
        public string Message { get; set; } = null;
    }

    /// <summary>What it can do, provided by implementations. Self explanatory.</summary>
    public class CommCaps
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
}
