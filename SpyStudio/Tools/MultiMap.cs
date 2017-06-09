using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpyStudio.Tools
{
    public class MultiMap<TKey, TValue>
    {
        private readonly Dictionary<TKey, List<TValue>> _map;

        public MultiMap()
        {
            _map = new Dictionary<TKey, List<TValue>>();
        }

        public MultiMap(IEqualityComparer<TKey> comp)
        {
            _map = new Dictionary<TKey, List<TValue>>(comp);
        }

        private List<TValue> InternalGet(TKey key)
        {
            List<TValue> ret;
            if (_map.TryGetValue(key, out ret))
                return ret;
            _map[key] = ret = new List<TValue>();
            return ret;
        }

        public IEnumerable<TValue> Get(TKey key)
        {
            return InternalGet(key);
        }

        public void Add(TKey key, TValue value)
        {
            InternalGet(key).Add(value);
        }
    }
}
