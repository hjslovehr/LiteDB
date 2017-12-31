﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB
{
    /// <summary>
    /// A class to control locking disposal states
    /// </summary>
    public class LockReadWrite : IDisposable
    {
        public ReaderWriterLockSlim Reader { get; private set; }
        public ReaderWriterLockSlim Header { get; set; } = null;
        public List<ReaderWriterLockSlim> Collections { get; set; } = new List<ReaderWriterLockSlim>();

        private Logger _log;

        internal LockReadWrite(ReaderWriterLockSlim reader, Logger log)
        {
            this.Reader = reader;
            _log = log;
        }

        /// <summary>
        /// When dipose, exit all lock states
        /// </summary>
        public void Dispose()
        {
            this.Reader.ExitReadLock();

            if (this.Header != null)
            {
                this.Header.ExitWriteLock();
            }

            foreach(var col in this.Collections)
            {
                col.ExitWriteLock();
            }
        }
    }
}