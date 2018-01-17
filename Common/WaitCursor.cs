using System;
using System.Windows.Forms;

namespace Nebulator.Common
{
    /// <summary>Class that provides a better wait cursor. Clients should use it with using (new WaitCursor()) { slow code }</summary>
    public class WaitCursor : IDisposable
    {
        /// <summary>Restore original cursor</summary>
        Cursor m_cursorOld;

        /// <summary>To detect redundant call</summary>
        bool _disposedValue = false;

        /// <summary>For metrics</summary>
        DateTime _start;

        /// <summary>Constructor</summary>
        public WaitCursor()
        {
            m_cursorOld = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            _start = DateTime.Now;
        }

        /// <summary>Dispose</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
            TimeSpan dur = DateTime.Now - _start;
        }

        /// <summary>Dispose</summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                Cursor.Current = m_cursorOld;
            }

            _disposedValue = true;
            TimeSpan dur = DateTime.Now - _start;
        }
    }
}
