using System;
using System.Collections.Generic;
using System.Text;

namespace Topten.RichTextKit.Utils
{
    /// <summary>
    /// A simple bi-directional dictionary
    /// </summary>
    /// <typeparam name="T1">Key type</typeparam>
    /// <typeparam name="T2">Value type</typeparam>
    class BiDictionary<T1, T2>
    {
        public Dictionary<T1, T2> Forward = new Dictionary<T1, T2>();
        public Dictionary<T2, T1> Reverse = new Dictionary<T2, T1>();

        public void Add(T1 key, T2 value)
        {
            Forward.Add(key, value);
            Reverse.Add(value, key);
        }

        public bool TryGetValue(T1 key, out T2 value)
        {
            return Forward.TryGetValue(key, out value);
        }

        public bool TryGetKey(T2 value, out T1 key)
        {
            return Reverse.TryGetValue(value, out key);
        }
    }
}
