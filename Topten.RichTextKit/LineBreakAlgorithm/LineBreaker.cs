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
            _first = true;
            _pos = 0;
            _lastPos = 0;
            _LB8a = false;
            _LB21a = false;
            _LB30a = 0;
        }

        Slice<int> _codePoints;
        bool _first = true;
        int _pos;
        int _lastPos;
        LineBreakClass _curClass;
        LineBreakClass _nextClass;
        bool _LB8a = false;
        bool _LB21a = false;
        int _LB30a = 0;

        /// <summary>
        /// Enumerate all line breaks
        /// </summary>
        /// <returns>A collection of line break positions</returns>
        public List<LineBreak> GetBreaks(bool mandatoryOnly = false)
        {
            var list = new List<LineBreak>();
            if (mandatoryOnly)
            {
                list.AddRange(FindMandatoryBreaks());
            }
            else
            {
                while (NextBreak(out var lb))
                    list.Add(lb);
            }
            return list;
        }

        LineBreakClass mapClass(LineBreakClass c)
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

        LineBreakClass mapFirst(LineBreakClass c)
        {
            switch (c)
            {
                case LineBreakClass.LF:
                case LineBreakClass.NL:
                    return LineBreakClass.BK;

                case LineBreakClass.SP:
                    return LineBreakClass.WJ;

                default:
                    return c;
            }
        }

        // Get the next character class
        LineBreakClass nextCharClass()
        {
            return mapClass(UnicodeClasses.LineBreakClass(_codePoints[_pos++]));
        }

        bool? getSimpleBreak()
        {
            // handle classes not handled by the pair table
            switch (_nextClass)
            {
                case LineBreakClass.SP:
                    return false;

                case LineBreakClass.BK:
                case LineBreakClass.LF:
                case LineBreakClass.NL:
                    _curClass = LineBreakClass.BK;
                    return false;

                case LineBreakClass.CR:
                    _curClass = LineBreakClass.CR;
                    return false;
            }

            return null;
        }

        bool getPairTableBreak(LineBreakClass lastClass)
        {
            // if not handled already, use the pair table
            bool shouldBreak = false;
            switch (LineBreakPairTable.table[(int)_curClass][(int)_nextClass])
            {
                case LineBreakPairTable.DI_BRK: // Direct break
                    shouldBreak = true;
                    break;

                case LineBreakPairTable.IN_BRK: // possible indirect break
                    shouldBreak = lastClass == LineBreakClass.SP;
                    break;

                case LineBreakPairTable.CI_BRK:
                    shouldBreak = lastClass == LineBreakClass.SP;
                    if (!shouldBreak)
                    {
                        shouldBreak = false;
                        return shouldBreak;
                    }
                    break;

                case LineBreakPairTable.CP_BRK: // prohibited for combining marks
                    if (lastClass != LineBreakClass.SP)
                    {
                        return shouldBreak;
                    }
                    break;

                case LineBreakPairTable.PR_BRK:
                    break;
            }

            if (_LB8a)
            {
                shouldBreak = false;
            }

            // Rule LB21a
            if (_LB21a && (_curClass == LineBreakClass.HY || _curClass == LineBreakClass.BA))
            {
                shouldBreak = false;
                _LB21a = false;
            }
            else
            {
                _LB21a = (_curClass == LineBreakClass.HL);
            }

            // Rule LB30a
            if (_curClass == LineBreakClass.RI)
            {
                _LB30a++;
                if (_LB30a == 2 && (_nextClass == LineBreakClass.RI))
                {
                    shouldBreak = true;
                    _LB30a = 0;
                }
            }
            else
            {
                _LB30a = 0;
            }

            _curClass = _nextClass;

            return shouldBreak;
        }


        public bool NextBreak(out LineBreak lineBreak)
        {
            // get the first char if we're at the beginning of the string
            if (_first)
            {
                _first = false;
                var firstClass = nextCharClass();
                _curClass = mapFirst(firstClass);
                _nextClass = firstClass;
                _LB8a = (firstClass == LineBreakClass.ZWJ);
                _LB30a = 0;
            }

            while (_pos < _codePoints.Length)
            {
                _lastPos = _pos;
                var lastClass = _nextClass;
                _nextClass = nextCharClass();

                // explicit newline
                if ((_curClass == LineBreakClass.BK) || ((_curClass == LineBreakClass.CR) && (_nextClass != LineBreakClass.LF)))
                {
                    _curClass = mapFirst(mapClass(_nextClass));
                    lineBreak = new LineBreak(findPriorNonWhitespace(_lastPos), _lastPos, true);
                    return true;
                }

                bool? shouldBreak = getSimpleBreak();

                if (!shouldBreak.HasValue)
                {
                    shouldBreak = getPairTableBreak(lastClass);
                }

                // Rule LB8a
                _LB8a = (_nextClass == LineBreakClass.ZWJ);

                if (shouldBreak.Value)
                {
                    lineBreak = new LineBreak(findPriorNonWhitespace(_lastPos), _lastPos, false);
                    return true;
                }
            }

            if (_lastPos < _codePoints.Length)
            {
                _lastPos = _codePoints.Length;
                var required = (_curClass == LineBreakClass.BK) || ((_curClass == LineBreakClass.CR) && (_nextClass != LineBreakClass.LF));
                lineBreak = new LineBreak(findPriorNonWhitespace(_codePoints.Length), _lastPos, required);
                return true;
            }
            else
            {
                lineBreak = new LineBreak(0, 0, false);
                return false;
            }
        }
        public IEnumerable<LineBreak> FindMandatoryBreaks()
        {
            for (int i = 0; i < _codePoints.Length; i++)
            {
                var cls = UnicodeClasses.LineBreakClass(_codePoints[i]);
                switch (cls)
                {
                    case LineBreakClass.BK:
                        yield return new LineBreak(i, i + 1, true);
                        break;

                    case LineBreakClass.CR:
                        if (i + 1 < _codePoints.Length && UnicodeClasses.LineBreakClass(_codePoints[i + 1]) == LineBreakClass.LF)
                        {
                            yield return new LineBreak(i, i + 2, true);
                        }
                        else
                        {
                            yield return new LineBreak(i, i + 1, true);
                        }
                        break;

                    case LineBreakClass.LF:
                        yield return new LineBreak(i, i + 1, true);
                        break;
                }
            }
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
    }
}
