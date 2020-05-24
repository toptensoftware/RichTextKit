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
                _current++;

            return _current < _end;
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
