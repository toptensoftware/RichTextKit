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
        Regional_Indicator = 5,
        Prepend = 6,
        SpacingMark = 7,
        L = 8,
        V = 9,
        T = 10,
        LV = 11,
        LVT = 12,
        ExtPict = 13,
        ZWJ = 14,

        // Pseudo classes, not generated from character data but used by pair table
        SOT = 15,
        EOT = 16,
        ExtPictZwg = 17,
    }
}
