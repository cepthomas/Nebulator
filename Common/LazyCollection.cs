using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nebulator.Common
{
    /// <summary>
    /// Helper collection class.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LazyCollection<T> where T : class
    {
        /// <summary>The internal collection.</summary>
        Dictionary<string, T> _items = new Dictionary<string, T>();

        /// <summary>Get all keys.</summary>
        public IEnumerable<string> Keys { get { return _items.Keys; } }

        /// <summary>Get all values.</summary>
        public IEnumerable<T> Values { get { return _items.Values; } }

        /// <summary>Allow replacement of existing items.</summary>
        public bool AllowOverwrite { get; set; } = false;

        /// <summary>
        /// Reset the collection.
        /// </summary>
        public void Clear()
        {
            _items.Clear();
        }

        /// <summary>
        /// Add a new value.
        /// </summary>
        /// <param name="name">Name key.</param>
        /// <param name="val">Value.</param>
        public void Add(string name, T val)
        {
            if (!_items.ContainsKey(name))
            {
                _items.Add(name, val);
            }
            else if(AllowOverwrite)
            {
                _items[name] = val;
            }
            else
            {
                throw new Exception("Collection already contains name " + name);
            }
        }

        /// <summary>
        /// Get the element.
        /// </summary>
        /// <param name="name">Which one.</param>
        /// <returns>The element or null if not in the collection.</returns>
        public T this[string name]
        {
            get
            {
                return _items.ContainsKey(name) ? _items[name] : null;
            }
            set
            {
                if (!_items.ContainsKey(name))
                {
                    _items.Add(name, value);
                }
                else
                {
                    _items[name] = value;
                }
            }
        }
    }
}
