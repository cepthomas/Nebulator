using System;
using System.Collections.Generic;
using System.IO;


namespace Nebulator.Common
{
    /// <summary>
    /// A watcher for multiple file changes. The underlying FileSystemWatcher is a bit sensitive to OS file system ops.
    /// </summary>
    public class MultiFileWatcher : IDisposable
    {
        #region Events
        /// <summary>Reporting a change to listeners.</summary>
        public event EventHandler<FileChangeEventArgs> FileChangeEvent;

        public class FileChangeEventArgs : EventArgs
        {
            public HashSet<string> FileNames { get; set; } = null;
        }
        #endregion

        #region Fields
        /// <summary>Detect changed composition file.</summary>
        List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();

        /// <summary>Used to delay reporting to client as there can be multiple events for one file change.</summary>
        System.Timers.Timer _timer = new System.Timers.Timer();

        /// <summary>Set by subordinate watchers.</summary>
        HashSet<string> _touchedFiles = new HashSet<string>();

        /// <summary>The delay before reporting. Seems like a reasonable number for human edit interface.</summary>
        const int DELAY = 100;

        /// <summary>Resource clean up.</summary>
        bool _disposed = false;
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public MultiFileWatcher()
        {
            _timer.Interval = DELAY;
            _timer.Enabled = true;
            _timer.Elapsed += Timer_Elapsed;
            _touchedFiles.Clear();
        }

        /// <summary>
        /// Handle timer tick. Sends event out if any constituents triggered.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if(_touchedFiles.Count > 0)
            {
                FileChangeEvent?.Invoke(this, new FileChangeEventArgs() { FileNames = _touchedFiles });
                _touchedFiles.Clear();
            }
        }

        /// <summary>
        /// Add anew listener.
        /// </summary>
        /// <param name="path"></param>
        public void Add(string path)
        {
            if(path != "")
            {
                FileSystemWatcher watcher = new FileSystemWatcher()
                {
                    Path = Path.GetDirectoryName(path),
                    Filter = Path.GetFileName(path),
                    EnableRaisingEvents = true,
                    NotifyFilter = NotifyFilters.LastWrite
                };

                watcher.Changed += Watcher_Changed;
            }
        }

        /// <summary>
        /// Remove all listeners.
        /// </summary>
        public void Clear()
        {
            _watchers.ForEach(w =>
            {
                w.Changed -= Watcher_Changed;
                w.Dispose();
            });
            _watchers.Clear();

            _touchedFiles.Clear();
            _timer.Interval = DELAY;
        }

        /// <summary>
        /// Handle underlying change notification.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            //NLog.LogManager.GetCurrentClassLogger().Info("Changed:" + e.FullPath);
            _touchedFiles.Add(e.FullPath);
            // Reset timer.
            _timer.Interval = DELAY;
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _watchers.ForEach(w => w.Dispose());
                _timer.Dispose();
                _disposed = true;
            }
        }
    }
}
