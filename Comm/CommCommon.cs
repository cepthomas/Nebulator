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
        public double MinVolume { get; set; }
        public double MaxVolume { get; set; }
        public double MinNote { get; set; }
        public double MaxNote { get; set; }
        public double MinControllerValue { get; set; }
        public double MaxControllerValue { get; set; }
        public double MinPitchValue { get; set; }
        public double MaxPitchValue { get; set; }
    }
}
