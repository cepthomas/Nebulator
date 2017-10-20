
using System;
using System.Collections.Generic;
using NLog;
using NLog.Targets;

namespace Nebulator.Common
{
    /// <summary>Handles writes to the client.</summary>
    [Target("ClientWindow")]
    public sealed class LogClientNotificationTarget : TargetWithLayout
    {
        #region Event Generation
        /// <summary>Definition of delegate for event handler.</summary>
        /// <param name="msg">The message to send</param>
        public delegate void ClientNotificationEventHandler(string msg);

        /// <summary>The event handler for messages back to the client.</summary>
        public static event ClientNotificationEventHandler ClientNotification;
        #endregion

        /// <summary>Send the event to the client for display.</summary>
        /// <param name="logEvent">Describes the event.</param>
        protected override void Write(LogEventInfo logEvent)
        {
            if (ClientNotification != null)
            {
                string preamble = "";
                if (logEvent.Level == LogLevel.Fatal || logEvent.Level == LogLevel.Error)
                {
                    preamble = "ERROR: ";
                }
                else if (logEvent.Level == LogLevel.Warn)
                {
                    preamble = "WARNING: ";
                }

                ClientNotification(preamble + logEvent.Message);
                //ClientNotification(logEvent.TimeStamp.ToString("yyyy'-'MM'-'dd HH':'mm':'ss.fff") + ": " + preamble + logEvent.Message);
            }
        }
    }
}
