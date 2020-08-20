// RichTextKit
// Copyright © 2019-2020 Topten Software. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License"), you may 
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

namespace Topten.RichTextKit
{
    /// <summary>
    /// Unicode grapheme cluster classes
    /// </summary>
    /// <remarks>
    /// Note, these need to match those used by the JavaScript script that
    /// generates the .trie resources
    /// </remarks>
    enum GraphemeClusterClass
    {
        Any = 0,
        CR = 1,
        LF = 2,
        Control = 3,
        Extend = 4,
        L = 5,
        V = 6,
        T = 7,
        LV = 8,
        LVT = 9,
        Prepend = 10,
        Regional_Indicator = 11,
        SpacingMark = 12,
        ZWJ = 13,
    }
}
