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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Topten.RichTextKit
{
    /// <summary>
    /// Represents a string decorated with rich text information including
    /// various helper methods for constructing rich text strings with a 
    /// fluent-like API.
    /// </summary>
    public class RichString
    {
        /// <summary>
        /// Constructs a new rich text string
        /// </summary>
        /// <param name="str">An initial piece of text to append to the string</param>
        public RichString(string str = null)
        {
            _paragraphs.Add(new ParagraphInfo());
            if (str != null)
                Add(str);
        }

        /// <summary>
        /// Append text to this RichString
        /// </summary>
        /// <param name="text">The text to append</param>
        /// <returns></returns>
        public RichString Add(string text) => Append(new TextItem(text));

        /// <summary>
        /// Adds text with various style attributes changed
        /// </summary>
        /// <param name="text">The text to append</param>
        /// <param name="fontFamily">The new font family</param>
        /// <param name="fontSize">The new font size</param>
        /// <param name="fontWeight">The new font weight</param>
        /// <param name="fontItalic">The new font italic</param>
        /// <param name="underline">The new underline style</param>
        /// <param name="strikeThrough">The new strike-through style</param>
        /// <param name="lineHeight">The new line height</param>
        /// <param name="textColor">The new text color</param>
        /// <param name="letterSpacing">The new character spacing</param>
        /// <param name="fontVariant">The new font variant</param>
        /// <param name="textDirection">The new text direction</param>
        /// <returns>A reference to the same RichString instance</returns>
        public RichString Add(string text,
           string fontFamily = null,
           float? fontSize = null,
           int? fontWeight = null,
           bool? fontItalic = null,
           UnderlineStyle? underline = null,
           StrikeThroughStyle? strikeThrough = null,
           float? lineHeight = null,
           SKColor? textColor = null,
           float? letterSpacing = null,
           FontVariant? fontVariant = null,
           TextDirection? textDirection = null
        )
        {
            if (string.IsNullOrEmpty(text))
                return this;

            Push();
            if (fontFamily != null) FontFamily(fontFamily);
            if (fontSize.HasValue) FontSize(fontSize.Value);
            if (fontWeight.HasValue) FontWeight(fontWeight.Value);
            if (fontItalic.HasValue) FontItalic(fontItalic.Value);
            if (underline.HasValue) Underline(underline.Value);
            if (strikeThrough.HasValue) StrikeThrough(strikeThrough.Value);
            if (lineHeight.HasValue) LineHeight(lineHeight.Value);
            if (textColor.HasValue) TextColor(textColor.Value);
            if (fontVariant.HasValue) FontVariant(fontVariant.Value);
            if (letterSpacing.HasValue) LetterSpacing(letterSpacing.Value);
            if (textDirection.HasValue) TextDirection(textDirection.Value);
            Add(text);
            Pop();

            return this;
        }


        /// <summary>
        /// Changes the font family
        /// </summary>
        /// <param name="value">The new font family</param>
        /// <returns>A reference to the same RichString instance</returns>
        public RichString FontFamily(string value) => Append(new FontFamilyItem(value));

        /// <summary>
        /// Changes the font size
        /// </summary>
        /// <param name="value">The new font size</param>
        /// <returns>A reference to the same RichString instance</returns>
        public RichString FontSize(float value) => Append(new FontSizeItem(value));

        /// <summary>
        /// Changes the font weight
        /// </summary>
        /// <param name="value">The new font weight</param>
        /// <returns>A reference to the same RichString instance</returns>
        public RichString FontWeight(int value) => Append(new FontWeightItem(value));

        /// <summary>
        /// Changes the font weight to bold or normal
        /// </summary>
        /// <param name="value">The new font bold setting</param>
        /// <returns>A reference to the same RichString instance</returns>
        public RichString Bold(bool value = true) => Append(new FontWeightItem(value ? 700 : 400));

        /// <summary>
        /// Changes the font italic setting 
        /// </summary>
        /// <param name="value">The new font italic setting</param>
        /// <returns>A reference to the same RichString instance</returns>
        public RichString FontItalic(bool value = true) => Append(new FontItalicItem(value));

        /// <summary>
        /// Changes the underline style
        /// </summary>
        /// <param name="value">The new underline style</param>
        /// <returns>A reference to the same RichString instance</returns>
        public RichString Underline(UnderlineStyle value = UnderlineStyle.Gapped) => Append(new UnderlineItem(value));

        /// <summary>
        /// Changes the strike-through style
        /// </summary>
        /// <param name="value">The new strike through style</param>
        /// <returns>A reference to the same RichString instance</returns>
        public RichString StrikeThrough(StrikeThroughStyle value = StrikeThroughStyle.Solid) => Append(new StrikeThroughItem(value));

        /// <summary>
        /// Changes the line height
        /// </summary>
        /// <param name="value">The new line height</param>
        /// <returns>A reference to the same RichString instance</returns>
        public RichString LineHeight(float value) => Append(new LineHeightItem(value));

        /// <summary>
        /// Changes the text color
        /// </summary>
        /// <param name="value">The new text color</param>
        /// <returns>A reference to the same RichString instance</returns>
        public RichString TextColor(SKColor value) => Append(new TextColorItem(value));

        /// <summary>
        /// Changes the character spacing
        /// </summary>
        /// <param name="value">The new character spacing</param>
        /// <returns>A reference to the same RichString instance</returns>
        public RichString LetterSpacing(float value) => Append(new LetterSpacingItem(value));

        /// <summary>
        /// Changes the font variant
        /// </summary>
        /// <param name="value">The new font variant</param>
        /// <returns>A reference to the same RichString instance</returns>
        public RichString FontVariant(FontVariant value) => Append(new FontVariantItem(value));

        /// <summary>
        /// Changes the text direction
        /// </summary>
        /// <param name="value">The new text direction</param>
        /// <returns>A reference to the same RichString instance</returns>
        public RichString TextDirection(TextDirection value) => Append(new TextDirectionItem(value));

        /// <summary>
        /// Saves the current style to an internal stack
        /// </summary>
        /// <returns>A reference to the same RichString instance</returns>
        public RichString Push() => Append(new PushItem());

        /// <summary>
        /// Resets to normal font (normal weight, italic off, underline off, strike through off, font variant off
        /// </summary>
        /// <returns>A reference to the same RichString instance</returns>
        public RichString Normal() => Append(new NormalItem());

        /// <summary>
        /// Restores a previous saved style from the internal stack
        /// </summary>
        /// <returns>A reference to the same RichString instance</returns>
        public RichString Pop() => Append(new PopItem());

        /// <summary>
        /// Starts a new text paragraph
        /// </summary>
        /// <returns>A reference to the same RichString instance</returns>
        public RichString Paragraph()
        {
            // End the previous paragraph with a carriage return
            Add("\n");

            // Start new paragraph
            _paragraphs.Add(new ParagraphInfo(_paragraphs[_paragraphs.Count - 1]));
            Invalidate();
            return this;
        }

        /// <summary>
        /// Sets the left margin of the current paragraph
        /// </summary>
        /// <param name="value">The margin width</param>
        /// <returns>A reference to the same RichString instance</returns>
        public RichString MarginLeft(float value)
        {
            _paragraphs[_paragraphs.Count - 1].MarginLeft = value;
            Invalidate();
            return this;
        }

        /// <summary>
        /// Sets the right margin of the current paragraph
        /// </summary>
        /// <param name="value">The margin width</param>
        /// <returns>A reference to the same RichString instance</returns>
        public RichString MarginRight(float value)
        {
            _paragraphs[_paragraphs.Count - 1].MarginRight = value;
            Invalidate();
            return this;
        }

        /// <summary>
        /// Sets the top margin of the current paragraph
        /// </summary>
        /// <param name="value">The margin height</param>
        /// <returns>A reference to the same RichString instance</returns>
        public RichString MarginTop(float value)
        {
            _paragraphs[_paragraphs.Count - 1].MarginTop = value;
            Invalidate();
            return this;
        }


        /// <summary>
        /// Sets the bottom margin of the current paragraph
        /// </summary>
        /// <param name="value">The margin height</param>
        /// <returns>A reference to the same RichString instance</returns>
        public RichString MarginBottom(float value)
        {
            _paragraphs[_paragraphs.Count - 1].MarginBottom = value;
            Invalidate();
            return this;
        }


        /// <summary>
        /// Sets the text alignment of the current paragraph
        /// </summary>
        /// <param name="value">The text alignment</param>
        /// <returns>A reference to the same RichString instance</returns>
        public RichString Alignment(TextAlignment value)
        {
            _paragraphs[_paragraphs.Count - 1].TextAlignment = value;
            Invalidate();
            return this;
        }

        /// <summary>
        /// Sets the base text direction of the current paragraph
        /// </summary>
        /// <param name="value">The base text direction</param>
        /// <returns>A reference to the same RichString instance</returns>
        public RichString BaseDirection(TextDirection value)
        {
            _paragraphs[_paragraphs.Count - 1].BaseDirection = value;
            Invalidate();
            return this;
        }

        /// <summary>
        /// The max width property sets the maximum width of a line, after which 
        /// the line will be wrapped onto the next line.
        /// </summary>
        /// <remarks>
        /// This property can be set to null, in which case lines won't be wrapped.
        /// </remarks>
        public float? MaxWidth
        {
            get => _maxWidth;
            set
            {
                if (value.HasValue && value.Value < 0)
                    value = 0;
                if (_maxWidth != value)
                {
                    _maxWidth = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// The maximum height of the TextBlock after which lines will be 
        /// truncated and the final line will be appended with an 
        /// ellipsis (`...`) character.
        /// </summary>
        /// <remarks>
        /// This property can be set to null, in which case the vertical height of the text block
        /// won't be capped.
        /// </remarks>
        public float? MaxHeight
        {
            get => _maxHeight;
            set
            {
                if (value.HasValue && value.Value < 0)
                    value = 0;

                if (value != _maxHeight)
                {
                    _maxHeight = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// The maximum number of lines after which lines will be 
        /// truncated and the final line will be appended with an 
        /// ellipsis (`...`) character.
        /// </summary>
        /// <remarks>
        /// This property can be set to null, in which case the vertical height of 
        /// the text block won't be capped.
        /// </remarks>
        public int? MaxLines
        {
            get => _maxLines;
            set
            {
                if (value.HasValue && value.Value < 0)
                    value = 0;

                if (value != _maxLines)
                {
                    _maxLines = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// Sets the default text alignment for cases where
        /// the rich text doesn't specify an alignment
        /// </summary>
        public TextAlignment DefaultAlignment
        {
            get => _textAlignment;
            set
            {
                if (_textAlignment != value)
                {
                    _textAlignment = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// The default base text direction for cases where the rich text
        /// doesn't explicitly specify a text direction
        /// </summary>
        public TextDirection DefaultDirection
        {
            get => _baseDirection;
            set
            {
                if (_baseDirection != value)
                {
                    _baseDirection = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// The default text style to be used as the current style at the start of the rich string.
        /// Subsequent formatting operations will be applied over this base style.
        /// </summary>
        public IStyle DefaultStyle
        {
            get => _baseStyle;
            set
            {
                if (!_baseStyle.IsSame(value))
                {
                    _needsFullLayout = true;
                    Invalidate();
                }
                _baseStyle = value;
            }
        }

        /// <summary>
        /// Paint this text block
        /// </summary>
        /// <param name="canvas">The Skia canvas to paint to</param>
        /// <param name="options">Options controlling the paint operation</param>
        public void Paint(
            SKCanvas canvas,
            TextPaintOptions options = null)
        {
            Paint(canvas, SKPoint.Empty, options);
        }

        /// <summary>
        /// Paint this text block
        /// </summary>
        /// <param name="canvas">The Skia canvas to paint to</param>
        /// <param name="position">The top left position within the canvas to draw at</param>
        /// <param name="options">Options controlling the paint operation</param>
        public void Paint(
        SKCanvas canvas,
        SKPoint position,
        TextPaintOptions options = null)
        {
            Layout();

            var ctx = new PaintContext()
            {
                owner = this,
                canvas = canvas,
                paintPosition = position,
                renderWidth = _maxWidth ?? _measuredWidth,
                textPaintOptions = options,
            };

            foreach (var p in _paragraphs)
            {
                p.Paint(ref ctx);
            }
        }

        /// <summary>
        /// Discards all internal layout structures
        /// </summary>
        public void DiscardLayout()
        {
            _needsLayout = true;
            foreach (var p in _paragraphs)
            {
                p.DiscardLayout();
            }
        }

        /// <summary>
        /// The total height of all lines.
        /// </summary>
        public float MeasuredHeight
        {
            get
            {
                Layout();
                return _measuredHeight;
            }
        }


        /// <summary>
        /// The width of the widest line of text.
        /// </summary>
        /// <remarks>
        /// The returned width does not include any overhang.
        /// </remarks>
        public float MeasuredWidth
        {
            get
            {
                Layout();
                return _measuredWidth;
            }
        }

        /// <summary>
        /// The number of lines in the text
        /// </summary>
        public int LineCount
        {
            get
            {
                Layout();
                return _measuredLines;
            }
        }

        /// <summary>
        /// Indicates if the text was truncated due to max height or max lines
        /// constraints
        /// </summary>
        public bool Truncated
        {
            get
            {
                Layout();
                return _truncated;
            }
        }

        /// <summary>
        /// Gets the total length of the string in code points
        /// </summary>
        public int Length
        {
            get
            {
                Layout();
                return _totalLength;
            }
        }

        /// <summary>
        /// Gets the measured length of the string up to the truncation point
        /// in code points
        /// </summary>
        public int MeasuredLength
        {
            get
            {
                Layout();
                return _measuredLength;
            }
        }

        /// <summary>
        /// Returns the revision number of the content of this rich text string
        /// </summary>
        /// <remarks>
        /// If the revision number of a text string has not changed then painting it 
        /// again will result in the exact same representation as the previous time.
        /// </remarks>
        public uint Revision
        {
            get
            {
                if (!_revisionValid)
                {
                    _revision++;
                    _revisionValid = true;
                }
                return _revision;
            }
        }

        /// <summary>
        /// Provides the plain-text equivalent of this RichString
        /// </summary>
        /// <returns>A plain-text string</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var p in _paragraphs)
            {
                p.Build(sb);
            }
            return sb.ToString();
        }


        /// <summary>
        /// Hit test this string
        /// </summary>
        /// <param name="x">The x-coordinate relative to top left of the string</param>
        /// <param name="y">The x-coordinate relative to top left of the string</param>
        /// <returns>A HitTestResult</returns>
        public HitTestResult HitTest(float x, float y)
        {
            // Make sure layout is up to date
            Layout();

            // Find the closest paragraph
            var para = FindClosestParagraph(y);

            // Get it's paint positio
            var paintPos = para.TextBlockPaintPosition(this);

            // Hit test
            var htr = para.TextBlock.HitTest(x - paintPos.X, y - paintPos.Y);

            // Convert the hit test record from TextBlock relative indices
            // to rich string relative indicies
            htr.ClosestLine += para.LineOffset;
            htr.ClosestCodePointIndex += para.CodePointOffset;
            if (htr.OverLine >= 0)
                htr.OverLine += para.LineOffset;
            if (htr.OverCodePointIndex >= 0)
                htr.OverCodePointIndex += para.CodePointOffset;

            // Done
            return htr;
        }

        ParagraphInfo FindClosestParagraph(float y)
        {
            // Work out which text block is closest
            ParagraphInfo pPrev = null;
            foreach (var p in _paragraphs)
            {
                // Ignore truncated paragraphs
                if (p.Truncated)
                    break;

                // Is it before this paragraph's text block?
                if (y < p.yPosition + p.MarginTop && pPrev != null)
                {
                    // Is it closer to this paragraph or the previous
                    // NB: We compare the text block coords, not the paragraphs
                    //     so that regardless of paragraph margins we always
                    //     hit test against the closer text block
                    var distPrev = y - (pPrev.yPosition + pPrev.TextBlock.MeasuredHeight);
                    var distThis = y - (p.yPosition + p.MarginTop);
                    if (Math.Abs(distPrev) < Math.Abs(distThis))
                        return pPrev;
                    else
                        return p;
                }

                // Is it within this paragraph's textblock?
                if (y < p.yPosition + p.MarginTop + p.TextBlock.MeasuredHeight)
                {
                    return p;
                }

                // Store the previous paragraph
                pPrev = p;
            }

            return pPrev;
        }


        /// <inheritdoc cref="TextBlock.GetCaretInfo(CaretPosition)"/>
        public CaretInfo GetCaretInfo(CaretPosition position)
        {
            Layout();

            // Is it outside the displayed range?
            if (position.CodePointIndex < 0 || position.CodePointIndex > MeasuredLength)
                return CaretInfo.None;

            // Find the paragraph containing that code point
            ParagraphInfo p;
            if (position.CodePointIndex == MeasuredLength)
            {
                // Special case for after the last displayed paragraph
                p = _paragraphs.LastOrDefault(x => !x.Truncated);
            }
            else
            {
                p = ParagraphForCodePointIndex(position.CodePointIndex);
            }


            // Get the caret info
            var ci = p.TextBlock.GetCaretInfo(new CaretPosition(position.CodePointIndex - p.CodePointOffset, position.AltPosition));

            // Adjust it
            ci.CodePointIndex += p.CodePointOffset;

            // Get it's paint position
            var paintPos = p.TextBlockPaintPosition(this);
            ci.CaretXCoord += paintPos.X;
            ci.CaretRectangle.Offset(paintPos);

            return ci;
        }

        ParagraphInfo ParagraphForCodePointIndex(int index)
        {
            for (int i=1; i<_paragraphs.Count; i++)
            {
                if (index < _paragraphs[i].CodePointOffset)
                    return _paragraphs[i - 1];
            }
            return _paragraphs[_paragraphs.Count - 1];
        }



        void Invalidate()
        {
            _needsLayout = true;
            _revisionValid = false;
        }

        void Layout()
        {
            // Full layout needed?
            if (_needsFullLayout)
            {
                _needsFullLayout = false;
                DiscardLayout();
            }

            // Needed?
            if (!_needsLayout)
                return;
            _needsLayout = false;

            // Create a layout context
            var lctx = new LayoutContext()
            {
                yPosition = 0,
                maxWidth = _maxWidth,
                maxHeight = _maxHeight,
                maxLines = _maxLines,
                textAlignment = _textAlignment,
                baseDirection = _baseDirection,
                styleManager = StyleManager.Default.Value,
                previousParagraph = null,
            };

            // Setup style manager
            lctx.styleManager.Reset();
            if (_baseStyle != null)
                lctx.styleManager.CurrentStyle = _baseStyle;

            // Layout each paragraph
            _measuredWidth = 0;
            foreach (var p in _paragraphs)
            {
                // Layout the paragraph
                p.Layout(ref lctx);

                // If this paragraph wasn't completely truncated, then update the measured width
                if (!p.Truncated)
                {
                    if (p.TextBlock.MeasuredWidth > _measuredWidth)
                        _measuredWidth = p.TextBlock.MeasuredWidth;
                }

                // Store the this paragraph as the previous so a fully truncated subsequent
                // paragraph can add the ellipsis to this one
                lctx.previousParagraph = p;
            }

            _measuredHeight = lctx.yPosition;
            _measuredLines = lctx.lineCount;
            _truncated = lctx.Truncated;
            _measuredLength = lctx.MeasuredLength;
            _totalLength = lctx.TotalLength;
        }

        struct PaintContext
        {
            public RichString owner;
            public SKCanvas canvas;
            public SKPoint paintPosition;
            public float renderWidth;
            public TextPaintOptions textPaintOptions;
        }

        struct LayoutContext
        {
            public float yPosition;
            public int lineCount;
            public bool Truncated;

            public float? maxWidth;
            public float? maxHeight;
            public int? maxLines;
            public TextAlignment? textAlignment;
            public TextDirection? baseDirection;
            public StyleManager styleManager;
            public ParagraphInfo previousParagraph;
            public int MeasuredLength;
            public int TotalLength;
        }



        // Append an item to the current paragraph
        RichString Append(Item item)
        {
            _paragraphs[_paragraphs.Count - 1]._items.Add(item);
            _needsFullLayout = true;
            Invalidate();
            return this;
        }

        bool _revisionValid = false;
        uint _revision = 0;
        bool _needsLayout = true;
        bool _needsFullLayout = true;
        float? _maxWidth;
        float? _maxHeight;
        int? _maxLines;
        TextAlignment _textAlignment;
        TextDirection _baseDirection;
        IStyle _baseStyle;

        float _measuredWidth;
        float _measuredHeight;
        int _measuredLines;
        bool _truncated;
        int _measuredLength;
        int _totalLength;

        List<ParagraphInfo> _paragraphs = new List<ParagraphInfo>();

        class ParagraphInfo
        {
            public ParagraphInfo()
            {
            }

            public ParagraphInfo(ParagraphInfo other)
            {
                MarginLeft = other.MarginLeft;
                MarginRight = other.MarginRight;
                MarginTop = other.MarginTop;
                MarginBottom = other.MarginBottom;
                TextAlignment = other.TextAlignment;
                BaseDirection = other.BaseDirection;
            }

            public void DiscardLayout()
            {
                TextBlock = null;
                Truncated = false;
            }

            // The position at which to paint this text block
            // relative to the top left of the entire string
            public SKPoint TextBlockPaintPosition(RichString owner)
            {
                // Adjust x-position according to resolved text alignment to prevent
                // having to re-calculate the TextBlock's layout
                float yPos = this.yPosition + MarginTop;
                float xPos = MarginLeft;
                if (!owner.MaxWidth.HasValue)
                {
                    switch (TextBlock.ResolveTextAlignment())
                    {
                        case RichTextKit.TextAlignment.Center:
                            xPos += (owner.MeasuredWidth - TextBlock.MeasuredWidth) / 2;
                            break;

                        case RichTextKit.TextAlignment.Right:
                            xPos += owner.MeasuredWidth - TextBlock.MeasuredWidth;
                            break;
                    }
                }
                return new SKPoint(xPos, yPos);
            }

            public void Build(StringBuilder sb)
            {
                foreach (var i in _items)
                {
                    i.Build(sb);
                }
            }

            public void Paint(ref PaintContext ctx)
            {
                if (Truncated)
                    return;

                TextRange? oldSel = null;
                if (ctx.textPaintOptions != null)
                {
                    // Save old selection ranges
                    oldSel = ctx.textPaintOptions.Selection;
                    if (ctx.textPaintOptions.Selection.HasValue)
                    {
                        ctx.textPaintOptions.Selection = ctx.textPaintOptions.Selection.Value.Offset(-this.CodePointOffset);
                    }
                }


                // Paint it
                TextBlock.Paint(ctx.canvas, ctx.paintPosition + TextBlockPaintPosition(ctx.owner), ctx.textPaintOptions);

                // Restore selection indicies
                if (oldSel.HasValue)
                {
                    ctx.textPaintOptions.Selection = oldSel;
                }
            }

            public int CodePointOffset;
            public int LineOffset;

            // Layout this paragraph
            public void Layout(ref LayoutContext ctx)
            {
                // Store y position of this block
                this.yPosition = ctx.yPosition;

                // Create the text block
                if (TextBlock == null)
                {
                    TextBlock = new TextBlock();
                    var buildContext = new BuildContext()
                    {
                        StyleManager = ctx.styleManager,
                        TextBlock = TextBlock,
                    };
                    foreach (var i in _items)
                    {
                        i.Build(buildContext);
                    }
                }
                
                // Store code point offset of this paragraph
                CodePointOffset = ctx.TotalLength;
                LineOffset = ctx.lineCount;
                ctx.TotalLength += TextBlock.Length;

                // Text already truncated?
                Truncated = ctx.Truncated;
                if (Truncated)
                    return;

                // Set the TextBlock
                TextBlock.Alignment = TextAlignment ?? ctx.textAlignment ?? RichTextKit.TextAlignment.Auto;
                TextBlock.BaseDirection = BaseDirection ?? ctx.baseDirection ?? RichTextKit.TextDirection.Auto;

                // Setup max width
                if (ctx.maxWidth.HasValue)
                {
                    TextBlock.MaxWidth = ctx.maxWidth.Value - (MarginLeft + MarginRight);
                }
                else
                {
                    TextBlock.MaxWidth = null;
                }

                // Set max height
                if (ctx.maxHeight.HasValue)
                {
                    TextBlock.MaxHeight = ctx.maxHeight.Value - (ctx.yPosition) - (MarginTop + MarginBottom);
                }
                else
                {
                    TextBlock.MaxHeight = null;
                }

                // Set max lines
                if (ctx.maxLines.HasValue)
                {
                    TextBlock.MaxLines = ctx.maxLines.Value - ctx.lineCount;
                }
                else
                {
                    TextBlock.MaxLines = null;
                }

                // Truncated?
                if (TextBlock.MaxLines == 0 || TextBlock.MaxHeight == 0)
                {
                    TextBlock = null;
                    Truncated = true;
                    ctx.Truncated = true;
                    return;
                }

                // Update the yPosition and stop further processing if truncated
                ctx.yPosition += TextBlock.MeasuredHeight + MarginTop;
                ctx.lineCount += TextBlock.Lines.Count;
                ctx.MeasuredLength += TextBlock.MeasuredLength;
                if (!TextBlock.Truncated)
                {
                    // Only add the bottom margin if it wasn't truncated
                    ctx.yPosition += MarginBottom;
                }
                else
                {
                    if (TextBlock.Lines.Count == 0 && ctx.previousParagraph != null)
                    {
                        ctx.previousParagraph.TextBlock.AddEllipsis();
                    }

                    // All following blocks should be truncated
                    ctx.Truncated = true;
                }
            }

            public TextBlock TextBlock;
            public float MarginLeft;
            public float MarginRight;
            public float MarginTop;
            public float MarginBottom;
            public TextAlignment? TextAlignment;
            public TextDirection? BaseDirection;
            public bool Truncated;
            public float yPosition;     // Laid out y-position

            public List<Item> _items = new List<Item>();
        }

        class BuildContext
        {
            public TextBlock TextBlock;
            public StyleManager StyleManager;
        }

        abstract class Item
        {
            public abstract void Build(BuildContext ctx);
            public virtual void Build(StringBuilder sb) { }
        }


        class TextItem : Item
        {
            public TextItem(string str)
            {
                _text = str;
            }

            string _text;

            public override void Build(BuildContext ctx)
            {
                ctx.TextBlock.AddText(_text, ctx.StyleManager.CurrentStyle);
            }

            public override void Build(StringBuilder sb)
            {
                sb.Append(_text);
            }
        }

        class FontFamilyItem : Item
        {
            public FontFamilyItem(string value)
            {
                _value = value;
            }

            string _value;

            public override void Build(BuildContext ctx)
            {
                ctx.StyleManager.FontFamily(_value);
            }
        }

        class FontSizeItem : Item
        {
            public FontSizeItem(float value)
            {
                _value = value;
            }

            float _value;

            public override void Build(BuildContext ctx)
            {
                ctx.StyleManager.FontSize(_value);
            }
        }

        class FontWeightItem : Item
        {
            public FontWeightItem(int value)
            {
                _value = value;
            }

            int _value;

            public override void Build(BuildContext ctx)
            {
                ctx.StyleManager.FontWeight(_value);
            }
        }

        class FontItalicItem : Item
        {
            public FontItalicItem(bool value)
            {
                _value = value;
            }

            bool _value;

            public override void Build(BuildContext ctx)
            {
                ctx.StyleManager.FontItalic(_value);
            }
        }

        class UnderlineItem : Item
        {
            public UnderlineItem(UnderlineStyle value)
            {
                _value = value;
            }

            UnderlineStyle _value;

            public override void Build(BuildContext ctx)
            {
                ctx.StyleManager.Underline(_value);
            }
        }

        class StrikeThroughItem : Item
        {
            public StrikeThroughItem(StrikeThroughStyle value)
            {
                _value = value;
            }

            StrikeThroughStyle _value;

            public override void Build(BuildContext ctx)
            {
                ctx.StyleManager.StrikeThrough(_value);
            }
        }

        class LineHeightItem : Item
        {
            public LineHeightItem(float value)
            {
                _value = value;
            }

            float _value;

            public override void Build(BuildContext ctx)
            {
                ctx.StyleManager.LineHeight(_value);
            }
        }

        class TextColorItem : Item
        {
            public TextColorItem(SKColor value)
            {
                _value = value;
            }

            SKColor _value;

            public override void Build(BuildContext ctx)
            {
                ctx.StyleManager.TextColor(_value);
            }
        }

        class LetterSpacingItem : Item
        {
            public LetterSpacingItem(float value)
            {
                _value = value;
            }

            float _value;

            public override void Build(BuildContext ctx)
            {
                ctx.StyleManager.LetterSpacing(_value);
            }
        }

        class FontVariantItem : Item
        {
            public FontVariantItem(FontVariant value)
            {
                _value = value;
            }

            FontVariant _value;

            public override void Build(BuildContext ctx)
            {
                ctx.StyleManager.FontVariant(_value);
            }
        }

        class TextDirectionItem : Item
        {
            public TextDirectionItem(TextDirection value)
            {
                _value = value;
            }

            TextDirection _value;

            public override void Build(BuildContext ctx)
            {
                ctx.StyleManager.TextDirection(_value);
            }
        }

        class PushItem : Item
        {
            public PushItem()
            {

            }

            public override void Build(BuildContext ctx)
            {
                ctx.StyleManager.Push();
            }
        }

        class PopItem : Item
        {
            public PopItem()
            {

            }

            public override void Build(BuildContext ctx)
            {
                ctx.StyleManager.Pop();
            }
        }

        class NormalItem : Item
        {
            public NormalItem()
            {

            }

            public override void Build(BuildContext ctx)
            {
                ctx.StyleManager.Bold(false);
                ctx.StyleManager.FontItalic(false);
                ctx.StyleManager.Underline(UnderlineStyle.None);
                ctx.StyleManager.StrikeThrough(StrikeThroughStyle.None);
                ctx.StyleManager.FontVariant(RichTextKit.FontVariant.Normal);
            }
        }


    }
}
