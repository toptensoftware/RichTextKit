// RichTextKit
// Copyright © 2019 Topten Software. All Rights Reserved.
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
//
// Ported from: https://github.com/foliojs/linebreak

using System.Collections.Generic;
using Topten.RichTextKit.Utils;

namespace Topten.RichTextKit
{
    /// <summary>
    /// Implementation of the Unicode Line Break Algorithm
    /// </summary>
    internal class LineBreaker
    {
        /// <summary>
        /// Constructor
        /// </summary>
        static LineBreaker()
        {
        }

        /// <summary>
        /// Reset this line breaker
        /// </summary>
        /// <param name="str">The string to be broken</param>
        public void Reset(string str)
        {
            Reset(new Slice<int>(Utf32Utils.ToUtf32(str)));
        }

        /// <summary>
        /// Reset this line breaker
        /// </summary>
        /// <param name="codePoints">The code points of the string to be broken</param>
        public void Reset(Slice<int> codePoints)
        {
            _codePoints = codePoints;
            _pos = 0;
            _lastPos = 0;
            _curClass = null;
            _nextClass = null;
        }

        /// <summary>
        /// Enumerate all line breaks (optionally in reverse order)
        /// </summary>
        /// <returns>A collection of line break positions</returns>
        public List<LineBreak> GetBreaks()
        {
            var list = new List<LineBreak>();
            while (NextBreak(out var lb))
                list.Add(lb);
            return list;
        }

        /// <summary>
        /// Get the next line break info
        /// </summary>
        /// <param name="lineBreak">A LineBreak structure returned by this method</param>
        /// <returns>True if there was another line break</returns>
        public bool NextBreak(out LineBreak lineBreak)
        {
            // get the first char if we're at the beginning of the string
            if (!_curClass.HasValue)
            {
                if (this.peekCharClass() == LineBreakClass.SP)
                    this._curClass = LineBreakClass.WJ;
                else                
                    this._curClass = mapFirst(this.readCharClass());
            }

            while (_pos < _codePoints.Length)
            {
                _lastPos = _pos;
                var lastClass = _nextClass;
                _nextClass = this.readCharClass();

                // explicit newline
                if (_curClass.HasValue && ((_curClass == LineBreakClass.BK) || ((_curClass == LineBreakClass.CR) && (this._nextClass != LineBreakClass.LF))))
                {
                    _curClass = mapFirst(mapClass(_nextClass.Value));
                    lineBreak = new LineBreak(findPriorNonWhitespace(_lastPos), _lastPos, true);
                    return true;
                }

                // handle classes not handled by the pair table
                LineBreakClass? cur = null;
                switch (_nextClass.Value)
                {
                    case LineBreakClass.SP:
                        cur = _curClass;
                        break;

                    case LineBreakClass.BK:
                    case LineBreakClass.LF:
                    case LineBreakClass.NL:
                        cur = LineBreakClass.BK;
                        break;

                    case LineBreakClass.CR:
                        cur = LineBreakClass.CR;
                        break;

                    case LineBreakClass.CB:
                        cur = LineBreakClass.BA;
                        break;
                }

                if (cur != null)
                {
                    _curClass = cur;
                    if (_nextClass.HasValue && _nextClass.Value == LineBreakClass.CB)
                    {
                        lineBreak = new LineBreak(findPriorNonWhitespace(_lastPos), _lastPos);
                        return true;
                    }
                    continue;
                }

                // if not handled already, use the pair table
                var shouldBreak = false;
                switch (LineBreakPairTable.table[(int)this._curClass.Value][(int)this._nextClass.Value])
                {
                    case LineBreakPairTable.DI_BRK: // Direct break
                        shouldBreak = true;
                        break;

                    case LineBreakPairTable.IN_BRK: // possible indirect break
                        shouldBreak = lastClass.HasValue && lastClass.Value == LineBreakClass.SP;
                        break;

                    case LineBreakPairTable.CI_BRK:
                        shouldBreak = lastClass.HasValue && lastClass.Value == LineBreakClass.SP;
                        if (!shouldBreak)
                        {
                            continue;
                        }
                        break;

                    case LineBreakPairTable.CP_BRK: // prohibited for combining marks
                        if (!lastClass.HasValue || lastClass.Value != LineBreakClass.SP)
                        {
                            continue;
                        }
                        break;
                }

                _curClass = _nextClass;
                if (shouldBreak)
                {
                    lineBreak = new LineBreak(findPriorNonWhitespace(_lastPos), _lastPos);
                    return true;
                }
            }

            if (_pos >= _codePoints.Length)
            {
                if (_lastPos < _codePoints.Length)
                {
                    _lastPos = _codePoints.Length;
                    var cls = UnicodeClasses.LineBreakClass(_codePoints[_codePoints.Length - 1]);
                    bool required = cls == LineBreakClass.BK || cls == LineBreakClass.LF || cls == LineBreakClass.CR;
                    lineBreak = new LineBreak(findPriorNonWhitespace(_codePoints.Length), _codePoints.Length, required);
                    return true;
                }
            }

            lineBreak = new LineBreak(0, 0, false);
            return false;
        }

        int findPriorNonWhitespace(int from)
        {
            if (from > 0)
            {
                var cls = UnicodeClasses.LineBreakClass(_codePoints[from - 1]);
                if (cls == LineBreakClass.BK || cls == LineBreakClass.LF || cls == LineBreakClass.CR)
                    from--;
            }
            while (from > 0)
            {
                var cls = UnicodeClasses.LineBreakClass(_codePoints[from - 1]);
                if (cls == LineBreakClass.SP)
                    from--;
                else
                    break;
            }
            return from;
        }

        int findNextNonWhitespace(int from)
        {
            while (from < _codePoints.Length && UnicodeClasses.LineBreakClass(_codePoints[from]) == LineBreakClass.SP)
                from++;
            return from;
        }

        // State
        Slice<int> _codePoints;
        int _pos;
        int _lastPos;
        LineBreakClass? _curClass;
        LineBreakClass? _nextClass;

        // Get the next character class
        LineBreakClass readCharClass()
        {
            return mapClass(UnicodeClasses.LineBreakClass(_codePoints[_pos++]));
        }

        LineBreakClass peekCharClass()
        {
            return mapClass(UnicodeClasses.LineBreakClass(_codePoints[_pos]));
        }



        static LineBreakClass mapClass(LineBreakClass c)
        {
            switch (c)
            {
                case LineBreakClass.AI:
                    return LineBreakClass.AL;

                case LineBreakClass.SA:
                case LineBreakClass.SG:
                case LineBreakClass.XX:
                    return LineBreakClass.AL;

                case LineBreakClass.CJ:
                    return LineBreakClass.NS;

                default:
                    return c;
            }
        }

        static LineBreakClass mapFirst(LineBreakClass c)
        {
            switch (c)
            {
                case LineBreakClass.LF:
                case LineBreakClass.NL:
                    return LineBreakClass.BK;

                case LineBreakClass.CB:
                    return LineBreakClass.BA;

                case LineBreakClass.SP:
                    return LineBreakClass.WJ;

                default:
                    return c;
            }
        }


    }
}
