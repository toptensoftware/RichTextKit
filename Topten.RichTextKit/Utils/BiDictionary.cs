// RichTextKit
// Copyright © 2019-2020 Topten Software. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may 
// not use this product except in compliance with the License. You may obtain 
// a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
// License for the specific language governing permissions and limitations 
// under the License.

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

        public void Clear()
        {
            Forward.Clear();
            Reverse.Clear();
        }

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

        public bool ContainsKey(T1 key)
        {
            return Forward.ContainsKey(key);
        }

        public bool ContainsValue(T2 value)
        {
            return Reverse.ContainsKey(value);
        }
    }
}
