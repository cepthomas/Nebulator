using System;

namespace Nebulator.FastTimer
{
    /// <summary>
    /// Interface for timer. Allows for experimentation with alternate (reliable) timer implementations.
    /// </summary>
    public interface IFastTimer : IDisposable
    {
        /// <summary>
        /// Gets or sets the time between Neb events in msec. 
        /// </summary>
        int NebPeriod { get; set; }

        /// <summary>
        /// Gets or sets the time between UI update events in msec.
        /// </summary>
        int UiPeriod { get; set; }

        /// <summary>
        /// Occurs when the time period has elapsed.
        /// </summary>
        event EventHandler<FastTimerEventArgs> TickEvent;

        /// <summary>
        /// Starts the timer.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops timer.
        /// </summary>
        void Stop();
    }

    /// <summary>
    /// FastTimer event args.
    /// </summary>
    public class FastTimerEventArgs : EventArgs
    {
        /// <summary>
        /// Neb update.
        /// </summary>
        public bool NebEvent { get; set; } = false;

        /// <summary>
        /// UI update.
        /// </summary>
        public bool UiEvent { get; set; } = false;
    }
}