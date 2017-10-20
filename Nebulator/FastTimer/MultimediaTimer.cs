using System;
using System.ComponentModel;
using System.Runtime.InteropServices;


namespace Nebulator.FastTimer
{
    /// <summary>
    /// Defines constants for the multimedia Timer's event types.
    /// </summary>
    enum TimerMode { OneShot, Periodic };

    /// <summary>
    /// Represents information about the multimedia Timer's capabilities.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    struct TimerCaps
    {
        /// <summary>
        /// Minimum supported period in milliseconds.
        /// </summary>
        public int periodMin;

        /// <summary>
        /// Maximum supported period in milliseconds.
        /// </summary>
        public int periodMax;

        public static TimerCaps Default
        {
            get
            {
                return new TimerCaps { periodMin = 1, periodMax = Int32.MaxValue };
            }
        }
    }

    /// <summary>
    /// Represents the Windows multimedia timer. Borrowed from Leslie Sanford.
    /// </summary>
    public class MultimediaTimer : IFastTimer
    {
        #region Properties
        /// <summary>
        /// Gets or sets the time between Neb events.
        /// </summary>
        public int NebPeriod
        {
            get
            {
                return _period;
            }
            set
            {
                _period = value;

                if (_running)
                {
                    Stop();
                    Start();
                }
            }
        }

        /// <summary>
        /// Gets or sets the time between UI update events.
        /// </summary>
        public int UiPeriod { get; set; }

        #endregion

        #region Fields
        /// <summary>
        /// Timer identifier.
        /// </summary>
        private int _timerID = -1;

        /// <summary>
        /// Timer mode.
        /// </summary>
        private TimerMode _mode = TimerMode.Periodic;

        /// <summary>
        /// Period between timer events in microseconds.
        /// </summary>
        private int _period = 100;

        /// <summary>
        /// Timer resolution in milliseconds. The resolution increases with smaller values - a resolution of 0
        /// indicates periodic events should occur with the greatest possible accuracy. To reduce system 
        /// overhead, however, you should use the maximum value appropriate for your application.
        /// </summary>
        private int _resolution = 1;

        /// <summary>
        /// Called by Windows when a timer periodic event occurs.
        /// </summary>
        private TimeProc _timeProcPeriodic;

        /// <summary>
        /// Called by Windows when a timer one shot event occurs.
        /// </summary>
        private TimeProc _timeProcOneShot;

        /// <summary>
        /// Indicates whether or not the timer is running.
        /// </summary>
        private bool _running = false;

        /// <summary>
        /// Indicates whether or not the timer has been disposed.
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// Multimedia timer capabilities.
        /// </summary>
        private static TimerCaps _caps;
        #endregion

        #region Events
        /// <summary>
        /// Occurs when the time period has elapsed.
        /// </summary>
        public event EventHandler<FastTimerEventArgs> TickEvent;
        #endregion

        #region Delegates
        /// <summary>
        /// Called by Windows when a timer event occurs.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="msg"></param>
        /// <param name="user"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        private delegate void TimeProc(int id, int msg, int user, int param1, int param2);

        /// <summary>
        /// Timer event.
        /// </summary>
        /// <param name="e"></param>
        private delegate void EventRaiser(EventArgs e);
        #endregion

        #region Win32 Multimedia Timer Functions
        [DllImport("winmm.dll")]
        private static extern int timeGetDevCaps(ref TimerCaps caps, int sizeOfTimerCaps);

        [DllImport("winmm.dll")]
        private static extern int timeSetEvent(int delay, int resolution, TimeProc proc, IntPtr user, int mode);

        [DllImport("winmm.dll")]
        private static extern int timeKillEvent(int id);

        private const int TIMERR_NOERROR = 0;
        #endregion

        #region Construction
        /// <summary>
        /// Initialize class.
        /// </summary>
        static MultimediaTimer()
        {
            // Get multimedia timer capabilities.
            timeGetDevCaps(ref _caps, Marshal.SizeOf(_caps));
        }

        /// <summary>
        /// Initializes a new instance of the Timer class.
        /// </summary>
        public MultimediaTimer()
        {
            // Initialize timer with default values.
            _timeProcPeriodic = new TimeProc(TimerPeriodicCallback);
            _timeProcOneShot = new TimeProc(TimerOneShotCallback);
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~MultimediaTimer()
        {
            if (_running)
            {
                // Stop and destroy timer.
                timeKillEvent(_timerID);
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Starts the timer.
        /// </summary>
        public void Start()
        {
            // If the periodic event callback should be used.
            if(_mode == TimerMode.Periodic)
            {
                // Create and start timer.
                _timerID = timeSetEvent(NebPeriod, _resolution, _timeProcPeriodic, IntPtr.Zero, (int)_mode);
            }
            // Else the one shot event callback should be used.
            else
            {
                // Create and start timer.
                _timerID = timeSetEvent(NebPeriod, _resolution, _timeProcOneShot, IntPtr.Zero, (int)_mode);
            }

            // If the timer was created successfully.
            if(_timerID != 0)
            {
                _running = true;
            }
            else
            {
                throw new Exception("Unable to start multimedia Timer.");
            }
        }

        /// <summary>
        /// Stops timer.
        /// </summary>
        public void Stop()
        {
            // Stop and destroy timer.
            int result = timeKillEvent(_timerID);

            // CET disabled: Debug.Assert(result == TIMERR_NOERROR);

            _running = false;
        }

        /// <summary>
        /// Callback method called by the Win32 multimedia timer when a timer periodic event occurs.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="msg"></param>
        /// <param name="user"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        private void TimerPeriodicCallback(int id, int msg, int user, int param1, int param2)
        {
            TickEvent?.Invoke(this, new FastTimerEventArgs() { NebEvent = true, UiEvent = true } );
        }

        /// <summary>
        /// Callback method called by the Win32 multimedia timer when a timer one shot event occurs.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="msg"></param>
        /// <param name="user"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        private void TimerOneShotCallback(int id, int msg, int user, int param1, int param2)
        {
            TickEvent?.Invoke(this, new FastTimerEventArgs() { NebEvent = true, UiEvent = true });
            Stop();
        }
        #endregion

        #region IDisposable Members
        /// <summary>
        /// Frees timer resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Frees timer resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Stop and destroy timer.
                    timeKillEvent(_timerID);
                }

                _disposed = true;
            }
        }
        #endregion
    }
}
