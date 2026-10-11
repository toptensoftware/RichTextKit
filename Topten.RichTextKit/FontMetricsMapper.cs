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

namespace Topten.RichTextKit
{
    public class VerticalFontMetrics
    {
        public float Ascent { get; }
        public float Descent { get; }
        public float Leading { get;}

        public VerticalFontMetrics(float ascent, float descent, float leading)
        {
            Ascent = ascent;
            Descent = descent;
            Leading = leading;
        }
    }

    /// <summary>
    /// The FontMetricsMapper class is responsible for mapping a font to a set of metrics
    /// which affect its layouting. The default instance can be replaced in cases where different
    /// layouting is desired.
    /// </summary>
    public class FontMetricsMapper
    {
        /// <summary>
        /// The default metrics mapper instance.
        /// </summary>
        public static FontMetricsMapper Default = new FontMetricsMapper();

        /// <summary>
        /// Maps a given typeface and font size to its vertical metrics.
        /// </summary>
        /// <param name="typeface">The typeface.</param>
        /// <param name="fontSize">The font size in pixels.</param>
        /// <returns>The vertical font metrics.</returns>
        public virtual VerticalFontMetrics GetVerticalMetrics(SKTypeface typeface, float fontSize)
        {
            var fontMetrics = typeface.ToFont(fontSize).Metrics;
            return new VerticalFontMetrics(fontMetrics.Ascent, fontMetrics.Descent, fontMetrics.Descent);
        }
    }
}
