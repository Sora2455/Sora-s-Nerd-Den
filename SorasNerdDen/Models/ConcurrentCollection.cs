using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace SorasNerdDen.Models
{
    public class ConcurrentCollection<T> : ICollection<T>, IDisposable
    {
        #region Fields
        private readonly List<T> _list;
        public readonly ReaderWriterLockSlim _lock;
        #endregion

        #region Constructors
        public ConcurrentCollection()
        {
            _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            _list = new List<T>();
        }

        public ConcurrentCollection(int capacity)
        {
            _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            _list = new List<T>(capacity);
        }

        public ConcurrentCollection(IEnumerable<T> items)
        {
            _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            _list = new List<T>(items);
        }
        #endregion

        #region Methods
        public void Add(T item)
        {
            try
            {
                _lock.EnterWriteLock();
                _list.Add(item);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool Remove(T item)
        {
            try
            {
                _lock.EnterWriteLock();
                return _list.Remove(item);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Clear()
        {
            try
            {
                _lock.EnterWriteLock();
                _list.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool Contains(T item)
        {
            try
            {
                _lock.EnterReadLock();
                return _list.Contains(item);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            try
            {
                _lock.EnterReadLock();
                _list.CopyTo(array, arrayIndex);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new ConcurrentEnumerator<T>(_list, _lock);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ConcurrentEnumerator<T>(_list, _lock);
        }

        public void Dispose()
        {
            _lock.Dispose();
        }
        #endregion

        #region Properties
        public int Count
        {
            get
            {
                try
                {
                    _lock.EnterReadLock();
                    return _list.Count;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }
        #endregion
    }

    public class ConcurrentEnumerator<T> : IEnumerator<T>
    {
        #region Fields
        private readonly IEnumerator<T> _inner;
        private readonly ReaderWriterLockSlim _lock;
        #endregion

        #region Constructor
        public ConcurrentEnumerator(IEnumerable<T> inner, ReaderWriterLockSlim @lock)
        {
            _lock = @lock;
            _lock.EnterReadLock();
            _inner = inner.GetEnumerator();
        }
        #endregion

        #region Methods
        public bool MoveNext()
        {
            return _inner.MoveNext();
        }

        public void Reset()
        {
            _inner.Reset();
        }

        public void Dispose()
        {
            _lock.ExitReadLock();
        }
        #endregion

        #region Properties
        public T Current
        {
            get { return _inner.Current; }
        }

        object IEnumerator.Current
        {
            get { return _inner.Current; }
        }
        #endregion
    }
}
