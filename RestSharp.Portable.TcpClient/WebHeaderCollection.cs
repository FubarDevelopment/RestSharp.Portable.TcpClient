using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace RestSharp.Portable.TcpClient
{
    internal class WebHeaderCollection : IDictionary<string, IList<string>>
    {
        private readonly Dictionary<string, IList<string>> _headers = new Dictionary<string, IList<string>>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, int> _headerOrder = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private int _orderIndex;

        public ICollection<string> Keys
        {
            get { return _headers.OrderBy(x => _headerOrder[x.Key]).Select(x => x.Key).ToList(); }
        }

        public ICollection<IList<string>> Values
        {
            get { return _headers.OrderBy(x => _headerOrder[x.Key]).Select(x => x.Value).ToList(); }
        }

        public int Count
        {
            get { return _headers.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public IList<string> this[string key]
        {
            get
            {
                IList<string> result;
                if (!_headers.TryGetValue(key, out result))
                    return null;
                return result;
            }
            set
            {
                if (!_headers.ContainsKey(key))
                {
                    Add(key, value ?? new List<string>());
                }
                else
                {
                    _headers[key] = value ?? new List<string>();
                }
            }
        }

        public string[] GetValues(string key)
        {
            var result = this[key];
            if (result == null)
                return null;
            return result.ToArray();
        }

        public void Add(string key, string value)
        {
            Add(
                key,
                new List<string>
                {
                    value,
                });
        }

        public void AddHeaders(HttpRequestMessage request)
        {
            HashSet<string> headerItemsInContent;
            if (request.Content != null)
            {
                headerItemsInContent = new HashSet<string>(
                    request.Content.Headers.Select(x => x.Key),
                    StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                headerItemsInContent = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            // Add all headers that are in the request that weren't added previously, but aren't in the content too.
            foreach (var header in request.Headers)
                if (!ContainsKey(header.Key) && !headerItemsInContent.Contains(header.Key))
                    Add(header.Key, header.Value.ToList());

            // Add all headers in the content that weren't added previously
            if (request.Content != null)
                foreach (var header in request.Content.Headers)
                    if (!ContainsKey(header.Key))
                        Add(header.Key, header.Value.ToList());
        }

        public void Add(string key, IEnumerable<string> value)
        {
            Add(key, value.ToList());
        }

        public void Add(string key, IList<string> value)
        {
            var newIndex = _orderIndex++;
            _headers.Add(key, value);
            _headerOrder.Add(key, newIndex);
        }

        public bool ContainsKey(string key)
        {
            return _headers.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            if (_headers.Remove(key))
            {
                _headerOrder.Remove(key);
                return true;
            }

            return false;
        }

        public bool TryGetValue(string key, out IList<string> value)
        {
            return _headers.TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<string, IList<string>> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _headers.Clear();
            _headerOrder.Clear();
        }

        public bool Contains(KeyValuePair<string, IList<string>> item)
        {
            return _headers.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, IList<string>>[] array, int arrayIndex)
        {
            var items = _headers.OrderBy(x => _headerOrder[x.Key]);
            var count = Math.Min(array.Length - arrayIndex, _headers.Count);

            var itemEnum = items.GetEnumerator();
            while (count-- != 0)
            {
                itemEnum.MoveNext();
                array[arrayIndex++] = itemEnum.Current;
            }
        }

        public bool Remove(KeyValuePair<string, IList<string>> item)
        {
            return Remove(item.Key);
        }

        public IEnumerator<KeyValuePair<string, IList<string>>> GetEnumerator()
        {
            return _headers.OrderBy(x => _headerOrder[x.Key]).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _headers.OrderBy(x => _headerOrder[x.Key]).GetEnumerator();
        }
    }
}
