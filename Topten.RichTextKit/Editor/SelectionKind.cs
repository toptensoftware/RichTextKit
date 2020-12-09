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

using SkiaSharp;

namespace Topten.RichTextKit.Editor
{
    /// <summary>
    /// Defines a kind of selection range
    /// </summary>
    public enum SelectionKind
    {
        /// <summary>
        /// No range
        /// </summary>
        None,

        /// <summary>
        /// Select a word
        /// </summary>
        Word,

        /// <summary>
        /// Select a line
        /// </summary>
        Line,

        /// <summary>
        /// Select a paragraph
        /// </summary>
        Paragraph,

        /// <summary>
        /// Select the entire document (ie: select all)
        /// </summary>
        Document,
    }
}
