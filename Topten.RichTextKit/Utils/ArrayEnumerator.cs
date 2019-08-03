using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Topten.RichTextKit
{
    class ArraySliceEnumerator<T> : IEnumerator<T>, IEnumerator
    {
        public ArraySliceEnumerator(T[] arr, int start, int length)
        {
            _arr = arr;
            _start = start;
            _end = start + length;
            _current = _start - 1;
        }

        T[] _arr;
        int _start;
        int _end;
        int _current;

        public T Current
        {
            get
            {
                if (_current < _end)
                    return _arr[_current];
                else
                    return default(T);
            }
        }


        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_current < _end)
            {
                _current++;
                return true;
            }
            else
                return false;
        }

        public void Reset()
        {
            _current = _start - 1;
        }

        public void Dispose()
        {
        }
    }
}
