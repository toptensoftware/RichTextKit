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
    /// Defines a kind of keyboard navigation
    /// </summary>
    public enum NavigationKind
    {
        /// <summary>
        /// No movement
        /// </summary>
        None,

        /// <summary>
        /// Move one character to the left
        /// </summary>
        CharacterLeft,

        /// <summary>
        /// Move one character to the right
        /// </summary>
        CharacterRight,

        /// <summary>
        /// Move up one line
        /// </summary>
        LineUp,
        
        /// <summary>
        /// Move down one line
        /// </summary>
        LineDown,

        /// <summary>
        /// Move left one word
        /// </summary>
        WordLeft,

        /// <summary>
        /// Move right one word
        /// </summary>
        WordRight,

        /// <summary>
        /// Move up one page
        /// </summary>
        PageUp,

        /// <summary>
        /// Move down one page
        /// </summary>
        PageDown,

        /// <summary>
        /// Move to the start of the line
        /// </summary>
        LineHome,

        /// <summary>
        /// Move to the end of the line
        /// </summary>
        LineEnd,

        /// <summary>
        /// Move to the top of the document
        /// </summary>
        DocumentHome,
        
        /// <summary>
        /// Move to the end of the document
        /// </summary>
        DocumentEnd,
    }
}
