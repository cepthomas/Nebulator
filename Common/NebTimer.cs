using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using System.Linq;


namespace Nebulator.Common
{
    /// <summary>
    /// FastTimer event args.
    /// </summary>
    public class NebTimerEventArgs : EventArgs
    {
        /// <summary>
        /// Elapsed timers.
        /// </summary>
        public List<string> ElapsedTimers { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents information about the multimedia timer capabilities.
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
    /// The win multimedia timer is erratic. This class attempts to reduce the error by running at one msec
    /// and managing the requested periods manually. This is accomplished by using a Stopwatch to actually
    /// measure the elapsed time rather than trust the mm timer period. It seems to be an improvement.
    /// Also see "Microsecond and Millisecond C# Timer - CodeProject.html". Good accuracy at the expense of a whole core.
    /// </summary>
    public class NebTimer// : IFastTimer
    {
        class TimerInstance
        {
            /// <summary>The time between events in msec.</summary>
            public int period = 10;

            /// <summary>Accumulated msec.</summary>
            public double current = 0.0;
        }

        #region Fields
        /// <summary>
        /// All the timer instances. Key is id.
        /// </summary>
        Dictionary<string, TimerInstance> _timers = new Dictionary<string, TimerInstance>();

        /// <summary>
        /// Used for more accurate timing.
        /// </summary>
        Stopwatch _sw = new Stopwatch();

        /// <summary>
        /// Indicates whether or not the timer is running.
        /// </summary>
        bool _running = false;

        /// <summary>
        /// Stopwatch support.
        /// </summary>
        long _lastTicks = -1;

        /// <summary>
        /// Indicates whether or not the timer has been disposed.
        /// </summary>
        bool _disposed = false;

        /// <summary>
        /// Msec for mm timer tick.
        /// </summary>
        const int MMTIMER_PERIOD = 1;
        #endregion

        #region Events
        /// <summary>
        /// Occurs when the time period has elapsed.
        /// </summary>
        public event EventHandler<NebTimerEventArgs> TimerElapsedEvent;
        #endregion

        #region Internal support for multimedia timer
        /// <summary>
        /// mm timer identifier.
        /// </summary>
        int _timerID = -1;

        /// <summary>
        /// Timer resolution in milliseconds. The resolution increases with smaller values - a resolution of 0
        /// indicates periodic events should occur with the greatest possible accuracy. To reduce system 
        /// overhead, however, you should use the maximum value appropriate for your application.
        /// </summary>
        int _resolution = 1;

        /// <summary>
        /// Called by Windows when a mm timer event occurs.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="msg"></param>
        /// <param name="user"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        delegate void TimeProc(int id, int msg, int user, int param1, int param2);

        /// <summary>
        /// Called by Windows when a mm timer event occurs.
        /// </summary>
        TimeProc _timeProc;
        #endregion

        #region Win32 Multimedia Timer Functions
        [DllImport("winmm.dll")]
        private static extern int timeGetDevCaps(ref TimerCaps caps, int sizeOfTimerCaps);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="delay">Event delay, in milliseconds.If this value is not in the range of the minimum and maximum event delays supported by the timer, the function returns an error.</param>
        /// <param name="resolution">Resolution of the timer event, in milliseconds. The resolution increases with smaller values; a resolution of 0 indicates periodic events should occur with the greatest possible accuracy. To reduce system overhead, however, you should use the maximum value appropriate for your application.</param>
        /// <param name="proc">Pointer to a callback function that is called once upon expiration of a single event or periodically upon expiration of periodic events.</param>
        /// <param name="user">User-supplied callback data.</param>
        /// <param name="mode">Timer event type.</param>
        /// <returns></returns>
        [DllImport("winmm.dll")]
        private static extern int timeSetEvent(int delay, int resolution, TimeProc proc, IntPtr user, int mode);

        [DllImport("winmm.dll")]
        private static extern int timeKillEvent(int id);

        private const int TIMERR_NOERROR = 0;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Initializes a new instance of the Timer class.
        /// </summary>
        public NebTimer()
        {
            if (!Stopwatch.IsHighResolution)
            {
                throw new Exception("High res performance counter is not available.");
            }

            // Initialize timer with default values.
            _timeProc = new TimeProc(MmTimerCallback);
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~NebTimer()
        {
            if (_running)
            {
                Stop();
            }
        }

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

        #region Methods
        /// <summary>
        /// Add a new timer instance.
        /// </summary>
        /// <param name="id">Arbitrary id as string</param>
        /// <param name="period"></param>
        public void SetTimer(string id, int period)
        {
            _timers[id] = new TimerInstance
            {
                period = period,
                current = 0
            };
        }

        /// <summary>
        /// Starts the periodic timer.
        /// </summary>
        public void Start()
        {
            foreach (TimerInstance t in _timers.Values)
            {
                t.current = 0;
            }

            // Create and start periodic timer.
            _timerID = timeSetEvent(MMTIMER_PERIOD, _resolution, _timeProc, IntPtr.Zero, 1); // TIME_PERIODIC

            // If the timer was created successfully.
            if (_timerID != 0)
            {
                _sw.Start();
                _running = true;
            }
            else
            {
                _running = false;
                throw new Exception("Unable to start periodic multimedia Timer.");
            }
        }

        /// <summary>
        /// Stops timer.
        /// </summary>
        public void Stop()
        {
            // Stop and destroy timer.
            int result = timeKillEvent(_timerID);
            _running = false;
            _sw.Stop();
        }

        /// <summary>
        /// Multimedia timer callback. Don't trust the accuracy of the mm timer so measure actual using a stopwatch.
        /// </summary>
        /// <param name="id">The identifier of the timer. The identifier is returned by the timeSetEvent function.</param>
        /// <param name="msg">Reserved.</param>
        /// <param name="user">The value that was specified for the dwUser parameter of the timeSetEvent function.</param>
        /// <param name="param1">Reserved.</param>
        /// <param name="param2">Reserved.</param>
        void MmTimerCallback(int id, int msg, int user, int param1, int param2)
        {
            if (_running)
            {
                if(_lastTicks != -1)
                {
                    // When are we?
                    long t = _sw.ElapsedTicks; // snap
                    double msec = (t - _lastTicks) * 1000D / Stopwatch.Frequency;
                    _lastTicks = t;

                    // Check for expirations.
                    List<string> elapsed = new List<string>();
                    // Allow for a bit of jitter around 0.
                    double allowance = 0.5; // msec

                    foreach(string tid in _timers.Keys)
                    {
                        TimerInstance timer = _timers[tid];
                        timer.current += msec;
                        if ((timer.period - timer.current) < allowance)
                        {
                            elapsed.Add(tid);
                            timer.current = 0.0;
                        }
                    }

                    if (elapsed.Count > 0)
                    {
                        TimerElapsedEvent?.Invoke(this, new NebTimerEventArgs() { ElapsedTimers = elapsed });
                    }
                }
                else
                {
                    // Starting.
                    _lastTicks = _sw.ElapsedTicks;
                }
            }
        }
        #endregion
    }
}
