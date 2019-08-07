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
