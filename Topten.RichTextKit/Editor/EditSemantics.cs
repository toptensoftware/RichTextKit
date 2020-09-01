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
    /// Defines various semantics for TextDocument edit operations
    /// </summary>
    public enum EditSemantics
    {
        /// <summary>
        /// No special behaviour
        /// </summary>
        None,

        /// <summary>
        /// Special behaviour for backspacing over one character
        /// </summary>
        Backspace,

        /// <summary>
        /// Special behaviour for forward deleting text one character
        /// </summary>
        ForwardDelete,

        /// <summary>
        /// Special behaviour typing text one character at time
        /// </summary>
        Typing,

        /// <summary>
        /// Special behaviour for overtyping existing text
        /// </summary>
        Overtype,

        /// <summary>
        /// Special behaviour for displaying the composition string of an IME
        /// </summary>
        ImeComposition,
    }
}
