using System;
using System.Collections.Generic;


namespace Nebulator.FastTimer
{
    /// <summary>
    /// Interface for timer. Allows for experimentation with alternate (reliable) timer implementations. TODO2 COnsolidate into one file.
    /// </summary>
    public interface IFastTimer : IDisposable
    {
        /// <summary>
        /// Set a timer value. Creates a new one if first set.
        /// </summary>
        /// <param name="id">Arbitrary id as string</param>
        /// <param name="period">Timer msec</param>
        /// <returns>Numerical id for this timer</returns>
        void SetTimer(string id, int period);

        /// <summary>
        /// Occurs when the time period has elapsed.
        /// </summary>
        event EventHandler<FastTimerEventArgs> TimerElapsedEvent;

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
        /// Elapsed timers.
        /// </summary>
        public List<string> ElapsedTimers { get; set; } = new List<string>();
    }
}