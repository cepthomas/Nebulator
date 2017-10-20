using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;


namespace Nebulator.FastTimer
{
    /// <summary>
    /// The win multimedia timer is erratic. This class attempts to reduce the error by running at one msec
    /// and managing the requested periods manually. This is accomplished by using a Stopwatch to actually
    /// measure the elapsed time rather than trust the mm timer period. It seems to be an improvement.
    /// This also paves the way for time wobbling and triplets.
    /// Perhaps I am asking too much of .NET... Might need to migrate the midi timing stuff to a C++ server.
    /// Also see "Microsecond and Millisecond C# Timer - CodeProject.html". Good accuracy at the expense of a whole core.
    /// </summary>
    public class NebTimer : IFastTimer
    {
        #region Properties
        /// <summary>
        /// Gets or sets the time between Neb events in msec. 
        /// </summary>
        public int NebPeriod { get; set; } = 10;

        /// <summary>
        /// Gets or sets the time between UI update events in msec.
        /// </summary>
        public int UiPeriod { get; set; } = 30;
        #endregion

        #region Fields
        /// <summary>
        /// Used for more accurate timing measurement.
        /// </summary>
        Stopwatch _sw = new Stopwatch();

        /// <summary>
        /// Indicates whether or not the timer is running.
        /// </summary>
        bool _running = false;

        /// <summary>
        /// Indicates whether or not the timer has been disposed.
        /// </summary>
        bool _disposed = false;

        /// <summary>
        /// Msec for mm timer tick.
        /// </summary>
        const int MMTIMER_PERIOD = 1;

        /// <summary>
        /// Accumulated neb msec.
        /// </summary>
        double _nebCurrent = 0.0;

        /// <summary>
        /// Accumulated UI msec.
        /// </summary>
        double _uiCurrent = 0.0;
        #endregion

        #region Events
        /// <summary>
        /// Occurs when the time period has elapsed.
        /// </summary>
        public event EventHandler<FastTimerEventArgs> TickEvent;

        /// <summary>
        /// Stopwatch support.
        /// </summary>
        long _lastTicks = -1;
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
        TimeProc _timeProc;

        /// <summary>
        /// Called by Windows when a mm timer event occurs.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="msg"></param>
        /// <param name="user"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        delegate void TimeProc(int id, int msg, int user, int param1, int param2);
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
        /// Starts the periodic timer.
        /// </summary>
        public void Start()
        {
            // Create and start periodic timer.
            _timerID = timeSetEvent(MMTIMER_PERIOD, _resolution, _timeProc, IntPtr.Zero, 1);

            _nebCurrent = 0;
            _uiCurrent = 0;

            // If the timer was created successfully.
            if (_timerID != 0)
            {
                _sw.Start();
                _running = true;
            }
            else
            {
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
        /// <param name="id"></param>
        /// <param name="msg"></param>
        /// <param name="user"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        void MmTimerCallback(int id, int msg, int user, int param1, int param2)
        {
            if(_running)
            {
                if(_lastTicks != -1)
                {
                    // When are we?
                    long t = _sw.ElapsedTicks; // snap
                    double msec = (t - _lastTicks) * 1000D / Stopwatch.Frequency;
                    _lastTicks = t;

                    // Check for expiration. Allow for a bit of jitter around 0.
                    bool nebExp = false;
                    bool uiExp = false;
                    _nebCurrent += msec;
                    _uiCurrent += msec;

                    if ((NebPeriod - _nebCurrent) < 0.5) //TODO correct?
                    {
                        nebExp = true;
                        _nebCurrent = 0.0;
                    }

                    if ((UiPeriod - _uiCurrent) < 0.5)
                    {
                        uiExp = true;
                        _uiCurrent = 0.0;
                    }

                    if (nebExp || uiExp)
                    {
                        TickEvent?.Invoke(this, new FastTimerEventArgs() { NebEvent = nebExp, UiEvent = uiExp });
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
