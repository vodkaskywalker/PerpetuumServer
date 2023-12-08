using System;
using System.Collections;
using System.Collections.Generic;

namespace Perpetuum.Collections
{
    public class LazyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly Lazy<IDictionary<TKey, TValue>> _dictionary;

        public LazyDictionary(Func<IDictionary<TKey, TValue>> dictionaryFactory)
        {
            _dictionary = new Lazy<IDictionary<TKey, TValue>>(dictionaryFactory);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _dictionary.Value.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            _dictionary.Value.Add(item);
        }

        public void Clear()
        {
            _dictionary.Value.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _dictionary.Value.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _dictionary.Value.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return _dictionary.Value.Remove(item);
        }

        public int Count
        {
            get { return _dictionary.Value.Count; }
        }

        public bool IsReadOnly
        {
            get { return _dictionary.Value.IsReadOnly; }
        }

        public bool ContainsKey(TKey key)
        {
            return _dictionary.Value.ContainsKey(key);
        }

        public void Add(TKey key, TValue value)
        {
            _dictionary.Value.Add(key, value);
        }

        public bool Remove(TKey key)
        {
            return _dictionary.Value.Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.Value.TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            get { return _dictionary.Value[key]; }
            set { _dictionary.Value[key] = value; }
        }

        public ICollection<TKey> Keys
        {
            get { return _dictionary.Value.Keys; }
        }

        public ICollection<TValue> Values
        {
            get { return _dictionary.Value.Values; }
        }
    }
}