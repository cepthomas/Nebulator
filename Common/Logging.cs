using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        /// <param name="level">Client might care about this</param>
        /// <param name="msg">The message to send</param>
        public delegate void ClientNotificationEventHandler(LogLevel level, string msg);

        /// <summary>The event handler for messages back to the client.</summary>
        public static event ClientNotificationEventHandler ClientNotification;
        #endregion

        /// <summary>Send the event to the client for display.</summary>
        /// <param name="logEvent">Describes the event.</param>
        protected override void Write(LogEventInfo logEvent)
        {
            // Can do some preprocessing here...

            //ClientNotification?.Invoke(logEvent.Level, logEvent.Message);

            var slog = Layout.Render(logEvent);
            ClientNotification?.Invoke(logEvent.Level, slog);
        }
    }
}