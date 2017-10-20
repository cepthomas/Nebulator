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
        Dictionary<string, T> _vals = new Dictionary<string, T>();

        /// <summary>Get all values.</summary>
        public IEnumerable<T> Values { get { return _vals.Values; } }

        /// <summary>Allow replacement of existing items.</summary>
        public bool AllowOverwrite { get; set; } = false;

        /// <summary>
        /// Reset the collection.
        /// </summary>
        public void Clear()
        {
            _vals.Clear();
        }

        /// <summary>
        /// Add a new value.
        /// </summary>
        /// <param name="name">Name key.</param>
        /// <param name="val">Value.</param>
        public void Add(string name, T val)
        {
            if (!_vals.ContainsKey(name))
            {
                _vals.Add(name, val);
            }
            else if(AllowOverwrite)
            {
                _vals[name] = val;
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
                return _vals.ContainsKey(name) ? _vals[name] : null;
            }
            set
            {
                if (!_vals.ContainsKey(name))
                {
                    _vals.Add(name, value);
                }
                else
                {
                    _vals[name] = value;
                }
            }
        }
    }
}
