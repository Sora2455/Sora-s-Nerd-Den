using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Common.Concurrency
{
    public class ConcurrentDictionaryOfCollections<TKey, TValue>
    {
        private ConcurrentDictionary<TKey, ConcurrentCollection<TValue>> Values { get; }

        public ConcurrentDictionaryOfCollections()
        {
            Values = new ConcurrentDictionary<TKey, ConcurrentCollection<TValue>>();
        }

        public IEnumerable<TValue> Get(TKey key)
        {
            if (Values.TryGetValue(key, out ConcurrentCollection<TValue> values))
            {
                return values;
            }
            return new TValue[0];
        }

        public List<TValue> GetAll()
        {
            return Values.Values.SelectMany(c => c).ToList();
        }

        public void Add(TKey key, TValue value)
        {
            Values.AddOrUpdate(key,
                new ConcurrentCollection<TValue>
                    {
                        value
                    },
                (_, oldCollection) =>
                {
                    oldCollection.Add(value);
                    return oldCollection;
                });
        }

        public void Remove(TKey key, TValue value)
        {
            if (Values.TryGetValue(key, out ConcurrentCollection<TValue> values))
            {
                try
                {
                    values._lock.EnterWriteLock();
                    values.RemoveNoLock(value);
                    if (values.CountNoLock == 0)
                    {
                        Values.TryRemove(key, out values);
                    }
                }
                finally
                {
                    values._lock.ExitWriteLock();
                }
            }
        }
    }
}
