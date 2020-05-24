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

//#define NO_POOLING

using System;
using System.Collections.Generic;
using System.Text;

namespace Topten.RichTextKit.Utils
{
    internal class ObjectPool<T> where T : class, new()
    {
        public ObjectPool()
        {
        }

        public T Get()
        {
#if NO_POOLING
            return new T();
#else
            int count = _pool.Count;
            if (count == 0)
                return new T();

            var obj = _pool[count - 1];
            _pool.RemoveAt(count - 1);
            return obj;
#endif
        }

        public void Return(T obj)
        {
#if NO_POOLING
#else
            Cleaner?.Invoke(obj);
            _pool.Add(obj);
#endif
        }

        public void Return(IEnumerable<T> objs)
        {
#if NO_POOLING
#else
            if (Cleaner != null)
            {
                foreach (var x in objs)
                {
                    Cleaner(x);
                }
                _pool.AddRange(objs);
            }
#endif
        }

        public void ReturnAndClear(List<T> objs)
        {
#if NO_POOLING
#else
            if (Cleaner != null)
            {
                foreach (var x in objs)
                {
                    Cleaner(x);
                }
                _pool.AddRange(objs);
            }
#endif
            objs.Clear();
        }

        public Action<T> Cleaner;

        List<T> _pool = new List<T>();
    }
}
