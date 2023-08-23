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
using System.Diagnostics;
using System.Linq;
using Topten.RichTextKit.Editor.UndoUnits;
using Topten.RichTextKit.Utils;

namespace Topten.RichTextKit.Editor
{
    /// <summary>
    /// Represents a the document part of a Document/View editor
    /// </summary>
    public partial class TextDocument
    {
        /// <summary>
        /// Constructs a new TextDocument
        /// </summary>
        public TextDocument()
        {
            // Create paragraph list
            _paragraphs = new List<Paragraph>();

            // Create our undo manager
            _undoManager = new UndoManager<TextDocument>(this);
            _undoManager.StartOperation += FireDocumentWillChange;
            _undoManager.EndOperation += FireDocumentDidChange;

            // Default margins
            MarginLeft = 3;
            MarginRight = 3;
            MarginTop = 3;
            MarginBottom = 3;

            // Temporary... add some text to work with
            _paragraphs.Add(new TextParagraph(_defaultStyle));
        }

        /// <summary>
        /// Set the document margins
        /// </summary>
        /// <remarks>
        /// This operation resets the undo manager
        /// </remarks>
        /// <param name="left">The left margin</param>
        /// <param name="top">The top margin</param>
        /// <param name="right">The right margin</param>
        /// <param name="bottom">The bottom margin</param>
        public void SetMargins(float left, float top, float right, float bottom)
        {
            MarginLeft = left;
            MarginTop = top;
            MarginRight = right;
            MarginBottom = bottom;
            InvalidateLayout();
            FireDocumentRedraw();
        }

        /// <summary>
        /// Specifies if the document is in single line mode 
        /// </summary>
        public bool SingleLineMode
        {
            get;
            set;
        }

        /// <summary>
        /// Specifies if the document is in plain text mode
        /// </summary>
        public bool PlainTextMode
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the default alignment for paragraphs in this document
        /// </summary>
        public TextAlignment DefaultAlignment
        {
            get => _defaultAlignment;
            set
            {
                _defaultAlignment = value;
                if (PlainTextMode)
                {
                    foreach (var p in _paragraphs)
                    {
                        p.TextBlock.Alignment = value;
                    }
                    InvalidateLayout();
                    FireDocumentRedraw();
                }
            }
        }

        /// <summary>
        /// Specifies the style to be used in plain text mode
        /// </summary>
        public IStyle DefaultStyle
        {
            get => _defaultStyle;
            set
            {
                _defaultStyle = value;
                if (PlainTextMode)
                {
                    foreach (var p in _paragraphs)
                    {
                        p.TextBlock.ApplyStyle(0, p.TextBlock.Length, _defaultStyle);
                    }
                    InvalidateLayout();
                    FireDocumentRedraw();
                }
            }
        }

        /// <summary>
        /// Get/set the entire document text
        /// </summary>
        public string Text
        {
            get
            {
                return this.GetText(new TextRange(0, Length)).ToString();
            }
            set
            {
                // Suppress normal events
                _suppressDocumentChangeEvents = true;

                // Update document text
                _paragraphs.Clear();
                _paragraphs.Add(new TextParagraph(_defaultStyle));
                ReplaceText(null, new TextRange(0, 0), value, EditSemantics.None);

                // Re-apply alignment
                DefaultAlignment = DefaultAlignment;

                // Disable undo
                _undoManager.Clear();

                // Will need layout update
                InvalidateLayout();

                // Restore events and fire a document reset event
                _suppressDocumentChangeEvents = false;
                FireDocumentReset();
            }
        }

        /// <summary>
        /// Registers a new view to receive notifications of changes to the document
        /// </summary>
        /// <param name="view">The view to be registered</param>
        public void RegisterView(ITextDocumentView view)
        {
            _views.Add(view);
        }

        /// <summary>
        /// Revokes a previously registered view
        /// </summary>
        /// <param name="view">The view to revoke</param>
        public void RevokeView(ITextDocumentView view)
        {
            _views.Remove(view);
        }

        /// <summary>
        /// Paint this text block
        /// </summary>
        /// <param name="canvas">The Skia canvas to paint to</param>
        /// <param name="fromYCoord">The top Y-Coord of the visible part of the document</param>
        /// <param name="toYCoord">The bottom Y-Coord of the visible part of the document</param>
        /// <param name="options">Options controlling the paint operation</param>
        public void Paint(SKCanvas canvas, float fromYCoord, float toYCoord, TextPaintOptions options = null)
        {
            // Make sure layout up to date
            Layout();

            // Find the starting paragraph
            int startParaIndex = _paragraphs.BinarySearch(fromYCoord, (para, a) =>
            {
                if (para.ContentYCoord > a)
                    return 1;
                if (para.ContentYCoord + para.ContentHeight < a)
                    return -1;
                return 0;
            });
            if (startParaIndex < 0)
                startParaIndex = ~startParaIndex;

            // Offset the selection range to be relative to the first paragraph
            // that will be painted
            if (options?.Selection != null)
            {
                if (startParaIndex == _paragraphs.Count)
                {
                    options.Selection = options.Selection.Value.Offset(-_totalLength);
                }
                else
                {
                    options.Selection = options.Selection.Value.Offset(-_paragraphs[startParaIndex].CodePointIndex);
                }
            }

            // Paint...  
            for (int i = startParaIndex; i < _paragraphs.Count; i++)
            {
                // Get the paragraph
                var p = _paragraphs[i];

                // Quit if past the region to be painted?
                if (p.ContentYCoord > toYCoord)
                    break;

                // Paint it
                p.Paint(canvas, options);

                // Update the selection range for the next paragraph
                if (options?.Selection != null)
                {
                    options.Selection = options.Selection.Value.Offset(-p.Length);
                }
            }
        }


        /// <summary>
        /// Indicates if text should be wrapped
        /// </summary>
        public bool LineWrap
        {
            get => _lineWrap;
            set
            {
                if (_lineWrap != value)
                {
                    _lineWrap = value;
                    InvalidateLayout();
                }
            }
        }

        /// <summary>
        /// Specifies the page width of the document
        /// </summary>
        /// <remarks>
        /// This value is ignored for single line editor
        /// </remarks>
        public float PageWidth
        {
            get => _pageWidth;
            set
            {
                if (_pageWidth != value)
                {
                    _pageWidth = value;
                    InvalidateLayout();
                }
            }
        }

        /// <summary>
        /// The document's left margin
        /// </summary>
        public float MarginLeft { get; private set; }

        /// <summary>
        /// The document's right margin
        /// </summary>
        public float MarginRight { get; private set; }

        /// <summary>
        /// The document's top margin
        /// </summary>
        public float MarginTop { get; private set; }

        /// <summary>
        /// The document's bottom margin
        /// </summary>
        public float MarginBottom { get; private set; }

        /// <summary>
        /// The total height of the document
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
        /// The total width of the document
        /// </summary>
        /// <remarks>
        /// For line-wrap documents this is the page width.
        /// For non-line-wrap documents this is the width of the widest paragraph.
        /// </remarks>
        public float MeasuredWidth
        {
            get
            {
                Layout();
                if (LineWrap)
                {
                    return _pageWidth;
                }
                else
                {
                    return _measuredWidth;
                }
            }
        }

        /// <summary>
        /// The total width of the content of the document
        /// </summary>
        /// <remarks>
        /// For line-wrap or non-line-wrap documents this is
        /// the width of the widest paragraph.
        /// </remarks>
        public float MeasuredContentWidth
        {
            get
            {
                Layout();
                return _measuredWidth;
            }
        }

        /// <summary>
        /// Gets the actual measured overhang in each direction based on the 
        /// fonts used, and the supplied text.
        /// </summary>
        /// <remarks>
        /// The return rectangle describes overhang amounts for each edge - not 
        /// rectangle co-ordinates.
        /// </remarks>
        public SKRect MeasuredOverhang
        {
            get
            {
                Layout();
                if (_paragraphs.Count == 0)
                    return new SKRect();

                var overhang = _paragraphs[0].TextBlock.MeasuredOverhang;
                float topOverhang = overhang.Top;
                float bottomOverhang = _paragraphs[_paragraphs.Count - 1].TextBlock.MeasuredOverhang.Bottom;
                float leftOverhang = overhang.Left;
                float rightOverhang = overhang.Right;
                for (int i = 1; i < _paragraphs.Count; i++)
                {
                    overhang = _paragraphs[i].TextBlock.MeasuredOverhang;
                    leftOverhang = Math.Max(leftOverhang, overhang.Left);
                    rightOverhang = Math.Max(rightOverhang, overhang.Right);
                }

                return new SKRect(leftOverhang, topOverhang, rightOverhang, bottomOverhang);
            }
        }

        /// <summary>
        /// Gets the total length of the document in code points
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
        /// Hit test this string
        /// </summary>
        /// <param name="x">The x-coordinate relative to top-left of the document</param>
        /// <param name="y">The y-coordinate relative to top-left of the document</param>
        /// <returns>A HitTestResult</returns>
        public HitTestResult HitTest(float x, float y)
        {
            // Find the closest paragraph
            var para = FindClosestParagraph(y);

            // Hit test the paragraph
            var htr = para.HitTest(x - para.ContentXCoord, y - para.ContentYCoord);

            // Text document doesn't support line indicies
            htr.ClosestLine = -1;
            htr.OverLine = -1;

            // Convert paragraph relative indicies to document relative indicies
            htr.ClosestCodePointIndex += para.CodePointIndex;
            if (htr.OverCodePointIndex >= 0)
                htr.OverCodePointIndex += para.CodePointIndex;

            // Done
            return htr;
        }

        /// <summary>
        /// Calculates useful information for displaying a caret
        /// </summary>
        /// <param name="position">The caret position</param>
        /// <returns>A CaretInfo struct, or CaretInfo.None</returns>
        public CaretInfo GetCaretInfo(CaretPosition position)
        {
            // Make sure layout up to date
            Layout();

            // Find the paragraph
            var paraIndex = GetParagraphForCodePointIndex(position, out var indexInParagraph);
            var para = _paragraphs[paraIndex];

            // Get caret info
            var ci = para.GetCaretInfo(new CaretPosition(indexInParagraph, position.AltPosition));

            // Adjust caret info to be relative to document
            ci.CodePointIndex += para.CodePointIndex;
            ci.CaretXCoord += para.ContentXCoord;
            ci.CaretRectangle.Offset(new SKPoint(para.ContentXCoord, para.ContentYCoord));
            ci.LineIndex = -1;      // Line numbers not supported on TextDocument

            // Done
            return ci;
        }

        /// <summary>
        /// Get the style of the text at a specified code point index
        /// </summary>
        /// <param name="offset">The offset of the code point</param>
        /// <returns>An IStyle</returns>
        public IStyle GetStyleAtOffset(int offset)
        {
            // Find containing paragraph
            var paraIndex = GetParagraphForCodePointIndex(new CaretPosition(offset), out var indexInPara);
            var para = _paragraphs[paraIndex];

            // Only support text blocks for now
            if (para.TextBlock == null)
                throw new NotImplementedException();

            // Get style from text block
            return para.TextBlock.GetStyleAtOffset(indexInPara);
        }

        /// <summary>
        /// Get the text for a part of the document
        /// </summary>
        /// <param name="range">The text to retrieve</param>
        /// <returns>The styled text</returns>
        public StyledText Extract(TextRange range)
        {
            var other = new StyledText();

            // Normalize and clamp range
            range = range.Normalized.Clamp(Length - 1);

            // Get all subruns
            foreach (var subrun in _paragraphs.GetInterectingRuns(range.Start, range.Length))
            {
                // Get the paragraph
                var para = _paragraphs[subrun.Index];
                if (para.TextBlock == null)
                    throw new NotImplementedException();

                var styledText = para.TextBlock.Extract(subrun.Offset, subrun.Length);
                foreach (var sr in styledText.StyleRuns)
                {
                    other.AddText(sr.CodePoints, sr.Style);
                }
            }

            // Convert paragraph separators to new lines
            other.CodePoints.Replace('\u2029', '\n');

            return other;
        }

        /// <summary>
        /// Handles keyboard navigation events
        /// </summary>
        /// <param name="position">The current caret position</param>
        /// <param name="kind">The direction and type of caret movement</param>
        /// <param name="pageSize">Specifies the page size for page up/down navigation</param>
        /// <param name="ghostXCoord">Transient storage for XCoord of caret during vertical navigation</param>
        /// <returns>The new caret position</returns>
        public CaretPosition Navigate(CaretPosition position, NavigationKind kind, float pageSize, ref float? ghostXCoord)
        {
            switch (kind)
            {
                case NavigationKind.None:
                    ghostXCoord = null;
                    return position;

                case NavigationKind.CharacterLeft:
                    ghostXCoord = null;
                    return navigateIndicies(-1, p => p.CaretIndicies);

                case NavigationKind.CharacterRight:
                    ghostXCoord = null;
                    return navigateIndicies(1, p => p.CaretIndicies);

                case NavigationKind.LineUp:
                    return navigateLine(-1, ref ghostXCoord);

                case NavigationKind.LineDown:
                    return navigateLine(1, ref ghostXCoord);

                case NavigationKind.WordLeft:
                    ghostXCoord = null;
                    return navigateIndicies(-1, p => p.WordBoundaryIndicies);

                case NavigationKind.WordRight:
                    ghostXCoord = null;
                    return navigateIndicies(1, p => p.WordBoundaryIndicies);

                case NavigationKind.PageUp:
                    return navigatePage(-1, ref ghostXCoord);

                case NavigationKind.PageDown:
                    return navigatePage(1, ref ghostXCoord);

                case NavigationKind.LineHome:
                    ghostXCoord = null;
                    return navigateLineEnd(-1);

                case NavigationKind.LineEnd:
                    ghostXCoord = null;
                    return navigateLineEnd(1);

                case NavigationKind.DocumentHome:
                    ghostXCoord = null;
                    return new CaretPosition(0);

                case NavigationKind.DocumentEnd:
                    ghostXCoord = null;
                    return new CaretPosition(Length, true);

                default:
                    throw new ArgumentException("Unknown navigation kind");
            }

            // Helper for character/word left/right
            CaretPosition navigateIndicies(int direction, Func<Paragraph, IReadOnlyList<int>> getIndicies)
            {
                // Get the paragraph and position in paragraph
                int paraIndex = GetParagraphForCodePointIndex(position, out var paraCodePointIndex);
                var para = _paragraphs[paraIndex];

                // Find the current caret index
                var indicies = getIndicies(para);
                var ii = indicies.BinarySearch(paraCodePointIndex);

                // Work out the new position
                if (ii < 0)
                {
                    ii = (~ii);
                    if (direction > 0)
                        ii--;
                }
                ii += direction;


                if (ii < 0)
                {
                    // Move to end of previous paragraph
                    if (paraIndex > 0)
                        return new CaretPosition(_paragraphs[paraIndex - 1].CodePointIndex + _paragraphs[paraIndex - 1].Length - 1);
                    else
                        return new CaretPosition(0);
                }

                if (ii >= indicies.Count)
                {
                    // Move to start of next paragraph
                    if (paraIndex + 1 < _paragraphs.Count)
                        return new CaretPosition(_paragraphs[paraIndex + 1].CodePointIndex);
                    else
                        return new CaretPosition(Length - 1);
                }

                // Move to new position in this paragraph
                var pos = para.CodePointIndex + indicies[ii];
                if (pos >= Length)
                    pos = Length - 1;
                return new CaretPosition(pos);
            }

            // Helper for line up/down
            CaretPosition navigateLine(int direction, ref float? xCoord)
            {
                // Get the paragraph and position in paragraph
                int paraIndex = GetParagraphForCodePointIndex(position, out var paraCodePointIndex);
                var para = _paragraphs[paraIndex];

                // Get the line number the caret is on
                var ci = para.GetCaretInfo(new CaretPosition(paraCodePointIndex, position.AltPosition));

                // Resolve the xcoord
                if (xCoord == null)
                    xCoord = ci.CaretXCoord + MarginLeft + para.MarginLeft;

                // Work out which line to hit test
                var lineIndex = ci.LineIndex + direction;

                // Exceed paragraph?
                if (lineIndex < 0)
                {
                    // Top of document?
                    if (paraIndex == 0)
                        return position;

                    // Move to last line of previous paragraph
                    para = _paragraphs[paraIndex - 1];
                    lineIndex = para.LineIndicies.Count - 1;
                }
                else if (lineIndex >= para.LineIndicies.Count)
                {
                    // End of document?
                    if (paraIndex + 1 >= _paragraphs.Count)
                        return position;

                    // Move to first line of next paragraph
                    para = _paragraphs[paraIndex + 1];
                    lineIndex = 0;
                }

                // Hit test the line
                var htr = para.HitTestLine(lineIndex, xCoord.Value - MarginLeft - para.MarginLeft);
                return new CaretPosition(para.CodePointIndex + htr.ClosestCodePointIndex, htr.AltCaretPosition);
            }

            // Helper for line home/end
            CaretPosition navigateLineEnd(int direction)
            {
                // Get the paragraph and position in paragraph
                int paraIndex = GetParagraphForCodePointIndex(position, out var paraCodePointIndex);
                var para = _paragraphs[paraIndex];

                // Get the line number the caret is on
                var ci = para.GetCaretInfo(new CaretPosition(paraCodePointIndex, position.AltPosition));

                // Get the line indicies
                var lineIndicies = para.LineIndicies;

                // Handle out of range
                if (ci.LineIndex < 0)
                    return new CaretPosition(para.CodePointIndex);

                if (direction < 0)
                {
                    // Return code point index of this line
                    return new CaretPosition(para.CodePointIndex + lineIndicies[ci.LineIndex]);
                }
                else
                {
                    // Last unwrapped line?
                    if (ci.LineIndex + 1 >= lineIndicies.Count)
                        return new CaretPosition(para.CodePointIndex + para.Length - 1);

                    // Return code point index of the next line, but with alternate caret position
                    // so caret appears at the end of this line
                    return new CaretPosition(para.CodePointIndex + lineIndicies[ci.LineIndex + 1], altPosition: true);
                }
            }

            // Helper for page up/down
            CaretPosition navigatePage(int direction, ref float? xCoord)
            {
                // Get current caret position
                var ci = this.GetCaretInfo(position);

                // Work out which XCoord to use
                if (xCoord == null)
                    xCoord = ci.CaretXCoord;

                // Hit test one page up
                var htr = this.HitTest(xCoord.Value, ci.CaretRectangle.MidY + pageSize * direction);

                // Convert to caret position
                return new CaretPosition(htr.ClosestCodePointIndex, htr.AltCaretPosition);
            }

        }

        /// <summary>
        /// Given a caret position, find an enclosing selection range for the
        /// current word, line, paragraph or document
        /// </summary>
        /// <param name="position">The caret position to select from</param>
        /// <param name="kind">The kind of selection to return</param>
        /// <returns></returns>
        public TextRange GetSelectionRange(CaretPosition position, SelectionKind kind)
        {
            switch (kind)
            {
                case SelectionKind.None:
                    return new TextRange(position.CodePointIndex, position.CodePointIndex, position.AltPosition);

                case SelectionKind.Word:
                    return getWordRange();

                case SelectionKind.Line:
                    return getLineRange();

                case SelectionKind.Paragraph:
                    return getParagraphRange();

                case SelectionKind.Document:
                    return new TextRange(0, Length, true);

                default:
                    throw new ArgumentException("Unknown navigation kind");
            }

            // Helper to get a word range
            TextRange getWordRange()
            {
                // Get the paragraph and position in paragraph
                int paraIndex = GetParagraphForCodePointIndex(position, out var paraCodePointIndex);
                var para = _paragraphs[paraIndex];

                // Find the word boundaries for this paragraph and find 
                // the current word
                var indicies = para.WordBoundaryIndicies;
                var ii = indicies.BinarySearch(paraCodePointIndex);
                if (ii < 0)
                    ii = (~ii - 1);
                if (ii >= indicies.Count)
                    ii = indicies.Count - 1;

                if (ii + 1 >= indicies.Count)
                {
                    // Point is past end of paragraph
                    return new TextRange(
                        para.CodePointIndex + indicies[ii],
                        para.CodePointIndex + indicies[ii],
                        true
                    );
                }

                // Create text range covering the entire word
                return new TextRange(
                    para.CodePointIndex + indicies[ii],
                    para.CodePointIndex + indicies[ii + 1],
                    true
                );
            }

            // Helper to get a line range
            TextRange getLineRange()
            {
                // Get the paragraph and position in paragraph
                int paraIndex = GetParagraphForCodePointIndex(position, out var paraCodePointIndex);
                var para = _paragraphs[paraIndex];

                // Get the line number the caret is on
                var ci = para.GetCaretInfo(new CaretPosition(paraCodePointIndex, position.AltPosition));

                // Get the line indicies
                var lineIndicies = para.LineIndicies;

                // Handle out of range (should never happen)
                if (ci.LineIndex < 0)
                    ci.LineIndex = 0;
                if (ci.LineIndex >= lineIndicies.Count)
                    ci.LineIndex = lineIndicies.Count - 1;

                // Return the line range
                if (ci.LineIndex + 1 < lineIndicies.Count)
                {
                    // Line within the paragraph
                    return new TextRange(
                        para.CodePointIndex + lineIndicies[ci.LineIndex],
                        para.CodePointIndex + lineIndicies[ci.LineIndex + 1],
                        true
                    );
                }
                else
                {
                    // Last line
                    return new TextRange(
                        para.CodePointIndex + lineIndicies[ci.LineIndex],
                        para.CodePointIndex + para.Length - 1,
                        true
                    );
                }
            }

            // Helper to get a paragraph range
            TextRange getParagraphRange()
            {
                // Get the paragraph and position in paragraph
                int paraIndex = GetParagraphForCodePointIndex(position, out var paraCodePointIndex);
                var para = _paragraphs[paraIndex];

                // Create text range covering the entire paragraph
                return new TextRange(
                    para.CodePointIndex,
                    para.CodePointIndex + para.Length - 1,
                    true
                );
            }
        }

        /// <summary>
        /// Get the undo manager for this document
        /// </summary>
        public UndoManager<TextDocument> UndoManager => _undoManager;

        /// <summary>
        /// Replaces a range of text with the specified text
        /// </summary>
        /// <param name="view">The view initiating the operation</param>
        /// <param name="range">The range to be replaced</param>
        /// <param name="text">The text to replace with</param>
        /// <param name="semantics">Controls how undo operations are coalesced and view selections updated</param>"
        /// <param name="styleToUse">The style to use for the added text (optional)</param>
        public void ReplaceText(ITextDocumentView view, TextRange range, string text, EditSemantics semantics, IStyle styleToUse = null)
        {
            // Convert text to utf32
            Slice<int> codePoints;
            if (!string.IsNullOrEmpty(text))
            {
                codePoints = new Utf32Buffer(text).AsSlice();
            }
            else
            {
                codePoints = Slice<int>.Empty;
            }

            // Do the work
            ReplaceText(view, range, codePoints, semantics, styleToUse);
        }

        /// <summary>
        /// Replaces a range of text with the specified text
        /// </summary>
        /// <param name="view">The view initiating the operation</param>
        /// <param name="range">The range to be replaced</param>
        /// <param name="codePoints">The text to replace with</param>
        /// <param name="semantics">Controls how undo operations are coalesced and view selections updated</param>"
        /// <param name="styleToUse">The style to use for the added text (optional)</param>
        public void ReplaceText(ITextDocumentView view, TextRange range, Slice<int> codePoints, EditSemantics semantics, IStyle styleToUse = null)
        {
            // Check range is valid
            if (range.Minimum < 0 || range.Maximum > this.Length)
                throw new ArgumentException("Invalid range", nameof(range));

            if (IsImeComposing)
                FinishImeComposition(view);

            // Convert new lines to paragraph separators
            if (PlainTextMode)
                codePoints.Replace('\n', '\u2029');

            // Break at the first line break
            if (SingleLineMode)
            {
                int breakPos = codePoints.IndexOfAny('\n', '\r', '\u2029');
                if (breakPos >= 0)
                    codePoints = codePoints.SubSlice(0, breakPos);
            }

            var styledText = new StyledText(codePoints);
            if (styleToUse != null)
            {
                styledText.ApplyStyle(0, styledText.Length, styleToUse);
            }
            ReplaceTextInternal(view, range, styledText, semantics, -1);
        }

        /// <summary>
        /// Replaces a range of text with the specified text
        /// </summary>
        /// <param name="view">The view initiating the operation</param>
        /// <param name="range">The range to be replaced</param>
        /// <param name="styledText">The text to replace with</param>
        /// <param name="semantics">Controls how undo operations are coalesced and view selections updated</param>"
        public void ReplaceText(ITextDocumentView view, TextRange range, StyledText styledText, EditSemantics semantics)
        {
            // Check range is valid
            if (range.Minimum < 0 || range.Maximum > this.Length)
                throw new ArgumentException("Invalid range", nameof(range));

            if (IsImeComposing)
                FinishImeComposition(view);

            // Convert new lines to paragraph separators
            if (PlainTextMode)
                styledText.CodePoints.Replace('\n', '\u2029');

            // Break at the first line break
            if (SingleLineMode)
            {
                int breakPos = styledText.CodePoints.SubSlice(0, styledText.Length).IndexOfAny('\n', '\r', '\u2029');
                if (breakPos >= 0)
                    styledText.DeleteText(breakPos, styledText.Length - breakPos);
            }

            ReplaceTextInternal(view, range, styledText, semantics, -1);
        }

        /// <summary>
        /// Indicates if an IME composition is currently in progress
        /// </summary>
        public bool IsImeComposing
        {
            get => _imeView != null;
        }


        /// <summary>
        /// Get the code point offset position of the current IME composition
        /// </summary>
        public int ImeCompositionOffset
        {
            get => _imeView == null ? -1 : _imeInitialSelection.Minimum;
        }

        /// <summary>
        /// Starts and IME composition action
        /// </summary>
        /// <param name="view">The initiating view</param>
        /// <param name="initialSelection">The initial text selection</param>
        public void StartImeComposition(ITextDocumentView view, TextRange initialSelection)
        {
            // Finish last composition
            if (_imeView != null)
                FinishImeComposition(view);

            // Store until first call
            _imeView = view;
            _imeInitialSelection = initialSelection;
        }

        /// <summary>
        /// Update a pending IME composition
        /// </summary>
        /// <param name="view">The initiating view</param>
        /// <param name="text">The composition text</param>
        /// <param name="caretOffset">The caret offset relative to the composition text</param>
        public void UpdateImeComposition(ITextDocumentView view, StyledText text, int caretOffset)
        {
            if (!IsImeComposing)
                return;

            ReplaceTextInternal(view, _imeInitialSelection, text, EditSemantics.ImeComposition, caretOffset);
        }

        /// <summary>
        /// Complete an IME composition
        /// </summary>
        /// <param name="view">The initiating view</param>
        public void FinishImeComposition(ITextDocumentView view)
        {
            if (_imeView != null)
            {
                Undo(view);
                _imeView = null;
            }
        }

        /// <summary>
        /// Undo the last editor operation
        /// </summary>
        /// <param name="view">The view initiating the undo command</param>
        public void Undo(ITextDocumentView view)
        {
            _initiatingView = view;
            _undoManager.Undo();
            _initiatingView = null;
        }

        /// <summary>
        /// Redo the undone edit operations
        /// </summary>
        /// <param name="view">The view initiating the redo command</param>
        public void Redo(ITextDocumentView view)
        {
            _initiatingView = view;
            _undoManager.Redo();
            _initiatingView = null;
        }

        /// <summary>
        /// Get the text for a part of the document
        /// </summary>
        /// <param name="range">The text to retrieve</param>
        /// <returns>The text as a Utf32Buffer</returns>
        public Utf32Buffer GetText(TextRange range)
        {
            // Normalize and clamp range
            range = range.Normalized.Clamp(Length - 1);

            // Get all subruns
            var buf = new Utf32Buffer();
            foreach (var subrun in _paragraphs.GetInterectingRuns(range.Start, range.Length))
            {
                // Get the paragraph
                var para = _paragraphs[subrun.Index];
                if (para.TextBlock == null)
                    throw new NotImplementedException();

                // Add the text
                buf.Add(para.TextBlock.CodePoints.SubSlice(subrun.Offset, subrun.Length));
            }

            // In plain text mode, replace paragraph separators with new line characters
            if (PlainTextMode)
            {
                buf.Replace('\u2029', '\n');
            }

            // In single line mode, stop at the first line break (which should be the only one at end)
            if (SingleLineMode)
            {
                int breakPos = Array.IndexOf(buf.Underlying, '\n');
                if (breakPos >= 0)
                {
                    buf.Delete(breakPos, buf.Length - breakPos);
                }
            }

            // Done!
            return buf;
        }

        /// <summary>
        /// Gets the range of text that will be overwritten by overtype mode
        /// at a particular location in the document
        /// </summary>
        /// <param name="range">The current selection range</param>
        /// <returns>The range that will be replaced by overtyping</returns>
        public TextRange GetOvertypeRange(TextRange range)
        {
            if (range.IsRange)
                return range;

            float? unused = null;
            var nextPos = Navigate(range.CaretPosition, NavigationKind.CharacterRight, 0, ref unused);

            var paraThis = GetParagraphForCodePointIndex(range.CaretPosition, out var _);
            var paraNext = GetParagraphForCodePointIndex(nextPos, out var _);

            if (paraThis == paraNext && nextPos.CodePointIndex < this.Length)
                range.End = nextPos.CodePointIndex;

            return range;
        }

        /// <summary>
        /// Given a code point index relative to the document, return which
        /// paragraph contains that code point and the offset within the paragraph
        /// </summary>
        /// <param name="position">The caret position to locate the paragraph for</param>
        /// <param name="indexInParagraph">Out parameter returning the code point index into the paragraph</param>
        /// <returns>The index of the paragraph</returns>
        int GetParagraphForCodePointIndex(CaretPosition position, out int indexInParagraph)
        {
            // Ensure layout is valid
            Layout();

            // Search paragraphs
            int paraIndex = _paragraphs.BinarySearch(position.CodePointIndex, (para, a) =>
            {
                if (a < para.CodePointIndex)
                    return 1;
                if (a >= para.CodePointIndex + para.Length)
                    return -1;
                return 0;
            });
            if (paraIndex < 0)
                paraIndex = ~paraIndex;

            // Clamp to end of document
            if (paraIndex >= _paragraphs.Count)
                paraIndex = _paragraphs.Count - 1;

            // Work out offset within paragraph
            indexInParagraph = position.CodePointIndex - _paragraphs[paraIndex].CodePointIndex;

            if (indexInParagraph == 0 && position.AltPosition && paraIndex > 0)
            {
                paraIndex--;
                indexInParagraph = _paragraphs[paraIndex].Length;
            }

            // Clamp to end of paragraph
            if (indexInParagraph > _paragraphs[paraIndex].Length)
                indexInParagraph = _paragraphs[paraIndex].Length;

            System.Diagnostics.Debug.Assert(indexInParagraph >= 0);

            // Done
            return paraIndex;
        }

        /// <summary>
        /// Helper to find the closest paragraph to a y-coordinate 
        /// </summary>
        /// <param name="y">Y-Coord to hit test</param>
        /// <returns>A reference to the closest paragraph</returns>
        Paragraph FindClosestParagraph(float y)
        {
            // Ensure layout is valid
            Layout();

            // Search paragraphs
            int paraIndex = _paragraphs.BinarySearch(y, (para, a) =>
            {
                if (para.ContentYCoord > a)
                    return 1;
                if (para.ContentYCoord + para.ContentHeight < a)
                    return -1;
                return 0;
            });

            // If in the vertical margin space between paragraphs, find the 
            // paragraph whose content is closest
            if (paraIndex < 0)
            {
                // Convert the paragraph index
                paraIndex = ~paraIndex;

                // Is it between paragraphs? 
                // (ie: not above the first or below the last paragraph)
                if (paraIndex > 0 && paraIndex < _paragraphs.Count)
                {
                    // Yes, find which paragraph's content the position is closer too
                    var paraPrev = _paragraphs[paraIndex - 1];
                    var paraNext = _paragraphs[paraIndex];
                    if (Math.Abs(y - (paraPrev.ContentYCoord + paraPrev.ContentHeight)) <
                        Math.Abs(y - paraNext.ContentYCoord))
                    {
                        return paraPrev;
                    }
                    else
                    {
                        return paraNext;
                    }
                }
            }

            // Clamp to last paragraph
            if (paraIndex >= _paragraphs.Count)
                paraIndex = _paragraphs.Count - 1;

            // Return the paragraph
            return _paragraphs[paraIndex];
        }


        /// <summary>
        /// Mark the document as needing layout update
        /// </summary>
        void InvalidateLayout()
        {
            _layoutValid = false;
        }

        /// <summary>
        /// Update the layout of the document
        /// </summary>
        void Layout()
        {
            // Already valid?
            if (_layoutValid)
                return;
            _layoutValid = true;

            // Work out the starting code point index and y-coord and starting margin
            float yCoord = 0;
            float prevYMargin = MarginTop;
            int codePointIndex = 0;

            _measuredWidth = 0;

            // Layout paragraphs
            for (int i = 0; i < _paragraphs.Count; i++)
            {
                // Get the paragraph
                var para = _paragraphs[i];

                // Layout
                para.Layout(this);

                // Position
                para.ContentXCoord = MarginLeft + para.MarginLeft;
                para.ContentYCoord = yCoord + Math.Max(para.MarginTop, prevYMargin);
                para.CodePointIndex = codePointIndex;

                // Width
                var paraWidth = para.ContentWidth + para.MarginLeft + para.MarginTop;
                if (paraWidth > _measuredWidth)
                    _measuredWidth = paraWidth;

                // Update positions
                yCoord = para.ContentYCoord + para.ContentHeight;
                prevYMargin = para.MarginBottom;
                codePointIndex += para.Length;
            }

            // Update the totals
            _measuredWidth += MarginLeft + MarginRight;
            _measuredHeight = yCoord + Math.Max(prevYMargin, MarginBottom);
            _totalLength = codePointIndex;
        }

        /// <summary>
        /// Notify all attached views that the document has been reset
        /// </summary>
        internal void FireDocumentReset()
        {
            for (int i = _views.Count - 1; i >= 0; i--)
            {
                _views[i].OnReset();
            }
        }

        /// <summary>
        /// Notify all attached views that the document has been reset
        /// </summary>
        internal void FireDocumentRedraw()
        {
            for (int i = _views.Count - 1; i >= 0; i--)
            {
                _views[i].OnRedraw();
            }
        }

        /// <summary>
        /// Notify all attached views that the document is about to change
        /// </summary>
        internal void FireDocumentWillChange()
        {
            if (_suppressDocumentChangeEvents)
                return;

            // Notify all views
            for (int i = _views.Count - 1; i >= 0; i--)
            {
                _views[i].OnDocumentWillChange(_initiatingView);
            }
        }

        /// <summary>
        /// Notify all attached views that the document has changed
        /// </summary>
        /// <param name="info">Info about the changes to the document</param>
        internal void FireDocumentChange(DocumentChangeInfo info)
        {
            if (_suppressDocumentChangeEvents)
                return;

            // Layout is now invalid
            InvalidateLayout();

            // Notify all views
            for (int i = _views.Count - 1; i >= 0; i--)
            {
                _views[i].OnDocumentChange(_initiatingView, info);
            }
        }

        /// <summary>
        /// Notify all attached views that the document has finished changing
        /// </summary>
        internal void FireDocumentDidChange()
        {
            if (_suppressDocumentChangeEvents)
                return;

            // Notify all views
            for (int i = _views.Count - 1; i >= 0; i--)
            {
                _views[i].OnDocumentDidChange(_initiatingView);
            }
        }



        /// <summary>
        /// Internal helper to replace text creating an undo unit
        /// </summary>
        /// <param name="view">The initiating view</param>
        /// <param name="range">The range of text to be replaced</param>
        /// <param name="text">The replacement text</param>
        /// <param name="semantics">The edit semantics of the change</param>
        /// <param name="imeCaretOffset">The position of the IME caret relative to the start of the range</param>
        void ReplaceTextInternal(ITextDocumentView view, TextRange range, StyledText text, EditSemantics semantics, int imeCaretOffset)
        {
            // Quit if redundant
            if (!range.IsRange && text.Length == 0)
                return;

            // Store the initiating view
            _initiatingView = view;

            // Make sure layout is up to date
            Layout();

            // Normalize the range
            range = range.Normalized;

            // Update range to include the following character if overtyping
            // and no current selection
            if (semantics == EditSemantics.Overtype && !range.IsRange)
            {
                range = GetOvertypeRange(range);
            }

            // Try to extend the last undo operation
            var group = _undoManager.GetUnsealedUnit() as UndoReplaceTextGroup;
            if (group != null && group.TryExtend(this, range, text, semantics, imeCaretOffset))
            {
                _initiatingView = null;
                return;
            }

            // Wrap all edits in an undo group.  Note this is a custom
            // undo group that also fires the DocumentChanged notification
            // to views.
            group = new UndoReplaceTextGroup();
            using (_undoManager.OpenGroup(group))
            {
                // Delete range (if any)
                if (range.Length != 0)
                {
                    DeleteInternal(range);
                }

                // Insert text (if any)
                if (text.Length != 0)
                {
                    InsertInternal(range.Minimum, text);
                }

                // Setup document change info on the group
                group.SetDocumentChangeInfo(new DocumentChangeInfo()
                {
                    CodePointIndex = range.Minimum,
                    OldLength = range.Normalized.Length,
                    NewLength = text.Length,
                    Semantics = semantics,
                    ImeCaretOffset = imeCaretOffset,
                });
            }

            _initiatingView = null;
        }

        /// <summary>
        /// Delete a section of the document
        /// </summary>
        /// <remarks>
        /// Returns the index of the first paragraph affected
        /// </remarks>
        /// <param name="range">The range to be deleted</param>
        int DeleteInternal(TextRange range)
        {
            // Iterate over the sections to be deleted
            int joinParagraph = -1;
            int firstParagraph = -1;
            foreach (var subRun in _paragraphs.GetIntersectingRunsReverse(range.Start, range.Length))
            {
                Debug.Assert(joinParagraph == -1);

                firstParagraph = subRun.Index;

                // Is it a partial paragraph deletion?
                if (subRun.Partial)
                {
                    // Yes...

                    // Get the paragraph
                    var para = _paragraphs[subRun.Index];

                    // Is it a text paragraph?
                    var textBlock = para.TextBlock;
                    if (textBlock != null)
                    {
                        // Yes

                        // If we're deleting paragraph separator at the the end 
                        // of this paragraph then remember this paragraph needs to 
                        // be joined with the next paragraph after any intervening 
                        // paragraphs have been deleted
                        if (subRun.Offset + subRun.Length >= para.Length)
                        {
                            Debug.Assert(joinParagraph == -1);
                            joinParagraph = subRun.Index;
                        }

                        // Delete the text
                        _undoManager.Do(new UndoDeleteText(textBlock, subRun.Offset, subRun.Length));
                    }
                    else
                    {
                        // No, todo...
                        throw new NotImplementedException();
                    }
                }
                else
                {
                    // Remove entire paragraph
                    _undoManager.Do(new UndoDeleteParagraph(subRun.Index));
                }
            }

            // If the deletion started mid paragraph and crossed into
            // subsequent paragraphs then we need to join the paragraphs
            if (joinParagraph >= 0 && joinParagraph + 1 < _paragraphs.Count)
            {
                // Get both paragraphs
                var firstPara = _paragraphs[joinParagraph];
                var secondPara = _paragraphs[joinParagraph + 1];

                // To join them, they must be both text paragraphs
                if (firstPara.TextBlock != null && secondPara.TextBlock != null)
                {
                    _undoManager.Do(new UndoJoinParagraphs(joinParagraph));
                }
            }

            // Layout is now invalid
            InvalidateLayout();

            return firstParagraph;
        }

        /// <summary>
        /// Insert text into the document
        /// </summary>
        /// <param name="position">The position to insert the text at</param>
        /// <param name="text">The text to insert</param>
        /// <returns>The index of the first paragraph affected</returns>
        int InsertInternal(int position, StyledText text)
        {
            // Find the position in the document
            var paraIndex = GetParagraphForCodePointIndex(new CaretPosition(position), out var indexInParagraph);
            var para = _paragraphs[paraIndex];

            // Is it a text paragraph?
            if (para.TextBlock == null)
            {
                // TODO:
                throw new NotImplementedException();
            }

            // Split the passed text into paragraphs
            var parts = text.CodePoints.GetRanges('\u2029').ToArray();
            if (parts.Length > 1)
            {
                // Split the paragraph at the insertion point into paragraphs A and B
                var paraA = para;
                var paraB = new TextParagraph(para as TextParagraph, indexInParagraph, para.Length);
                if (para.TextBlock.Length - indexInParagraph - 1 != 0)
                    _undoManager.Do(new UndoDeleteText(paraA.TextBlock, indexInParagraph, para.TextBlock.Length - indexInParagraph - 1));

                // Append the first part of the inserted text to the end of paragraph A
                var firstPart = parts[0];
                if (firstPart.Length != 0)
                    _undoManager.Do(new UndoInsertText(paraA.TextBlock, indexInParagraph, text.Extract(firstPart.Offset, firstPart.Length)));

                // Prepend the last text part of the inserted text to the start paragraph B
                var lastPart = parts[parts.Length - 1];
                if (lastPart.Length != 0)
                    _undoManager.Do(new UndoInsertText(paraB.TextBlock, 0, text.Extract(lastPart.Offset, lastPart.Length)));

                // We could do this above, but by doing it after the above InsertText operations
                // we prevent subsequent typing from be coalesced into this unit.
                _undoManager.Do(new UndoInsertParagraph(paraIndex + 1, paraB));

                // Create new paragraphs for parts [1..N-1] of the inserted text and insert them
                // betweeen paragraphs A and B.
                for (int i = 1; i < parts.Length - 1; i++)
                {
                    var betweenPara = new TextParagraph(para as TextParagraph, para.Length - 1, 1);
                    var part = parts[i];
                    betweenPara.TextBlock.InsertText(0, text.Extract(part.Offset, part.Length));
                    _undoManager.Do(new UndoInsertParagraph(paraIndex + i, betweenPara));
                }
            }
            else
            {
                _undoManager.Do(new UndoInsertText(para.TextBlock, indexInParagraph, text));
            }

            return paraIndex;
        }

        /// Private members
        float _pageWidth = 1000;            // Arbitary default
        float _measuredHeight = 0;
        float _measuredWidth = 0;
        int _totalLength = 0;
        bool _layoutValid = false;
        bool _lineWrap = true;
        internal List<Paragraph> _paragraphs;

        UndoManager<TextDocument> _undoManager;
        List<ITextDocumentView> _views = new List<ITextDocumentView>();
        ITextDocumentView _initiatingView;
        ITextDocumentView _imeView;
        TextRange _imeInitialSelection;
        IStyle _defaultStyle = StyleManager.Default.Value.DefaultStyle;
        TextAlignment _defaultAlignment = TextAlignment.Left;
        bool _suppressDocumentChangeEvents = false;
    }
}
