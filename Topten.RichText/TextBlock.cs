using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Topten.RichText
{
    /// <summary>
    /// Represents a block of formatted, laid out and measurable text
    /// </summary>
    public class TextBlock
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public TextBlock()
        {
        }

        /// <summary>
        /// The width of available space for layout.  Set to null to disable line wrapping
        /// </summary>
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
                    InvalidateLayout();
                }
            }
        }

        /// <summary>
        /// The height of available space for layout (set to null to disable ellipsis cropping)
        /// </summary>
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
                    InvalidateLayout();
                }
            }
        }

        /// <summary>
        /// The maximum number of allowed lines (set to null to disable ellipsis cropping)
        /// </summary>
        public int? MaxLines
        {
            get => _maxLinesResolved;
            set
            {
                if (value.HasValue && value.Value < 1)
                    value = 1;

                if (value != _maxLines)
                {
                    _maxLines = value;
                    InvalidateLayout();
                }
            }
        }

        /// <summary>
        /// Set the left/right/center alignment of text in this paragraph
        /// </summary>
        public TextAlignment Alignment
        {
            get => _textAlignment;
            set
            {
                if (_textAlignment != value)
                {
                    _textAlignment = value;
                    InvalidateLayout();
                }
            }
        }

        /// <summary>
        /// The base directionality (whether text is laid out left to right, or right to left)
        /// </summary>
        public TextDirection BaseDirection
        {
            get => _baseDirection;
            set
            {
                if (_baseDirection != value)
                {
                    _baseDirection = value;
                    InvalidateLayout();
                }
            }
        }

        /// <summary>
        /// Use MS Word style RTL layout
        /// </summary>
        public bool UseMSWordStyleRTLLayout
        {
            get => _useMSWordStyleRtlLayout;
            set
            {
                if (_useMSWordStyleRtlLayout != value)
                {
                    _useMSWordStyleRtlLayout = value;
                    InvalidateLayout();
                }
            }
        }

        /// <summary>
        /// Clear the content of this text block
        /// </summary>
        public void Clear()
        {
            // Reset everything
            _codePoints.Clear();
            _styledRuns.Clear();
            _fontRuns.Clear();
            _lines.Clear();
            InvalidateLayout();
        }

        /// <summary>
        /// Add text to this paragraph
        /// </summary>
        /// <param name="text">The text to add</param>
        /// <param name="style">The style of the text</param>
        public StyledRun AddText(string text, IStyle style)
        {
            // Quit if redundant
            if (string.IsNullOrEmpty(text))
                return null;

            // Add to  buffer
            var utf32 = _codePoints.Add(text);

            // Create a run
            var run = new StyledRun()
            {
                TextBlock = this,
                CodePointBuffer = _codePoints,
                Start = utf32.Start,
                Length = utf32.Length,
                Style = style,
            };

            // Add run
            _styledRuns.Add(run);

            return run;
        }

        /// <summary>
        /// Add text to this paragraph
        /// </summary>
        /// <param name="text">The text to add</param>
        /// <param name="style">The style of the text</param>
        public StyledRun AddText(Slice<int> text, IStyle style)
        {
            if (text.Length == 0)
                return null;

            // Add to UTF-32 buffer
            var utf32 = _codePoints.Add(text);

            // Create a run
            var run = new StyledRun()
            {
                TextBlock = this,
                CodePointBuffer = _codePoints,
                Start = utf32.Start,
                Length = utf32.Length,
                Style = style,
            };

            // Add run
            _styledRuns.Add(run);

            return run;
        }

        /// <summary>
        /// Lays out the provided text and returns paragraph
        /// </summary>
        /// <returns>A paragraph that can be drawn</returns>
        public void Layout()
        {
            // Needed?
            if (!_needsLayout)
                return;
            _needsLayout = false;

            // Resolve max width/height
            _maxWidthResolved = _maxWidth ?? float.MaxValue;
            _maxHeightResolved = _maxHeight ?? float.MaxValue;
            _maxLinesResolved = _maxLines ?? int.MaxValue;

            // Reset layout state
            _fontRuns.Clear();
            _lines.Clear();
            _cursorIndicies.Clear();
            _measuredHeight = 0;
            _measuredWidth = 0;
            _minLeftMargin = 0;
            _requiredLeftMargin = null;

            // Build font runs
            BuildFontRuns();

            // Break font runs into lines
            BreakLines();

            // Finalize lines
            FinalizeLines();
        }

        /// <summary>
        /// Get the text runs as added by AddText
        /// </summary>
        public IReadOnlyList<StyledRun> StyledRuns
        {
            get
            {
                return _styledRuns;
            }
        }

        /// <summary>
        /// Get all font runs for this text block
        /// </summary>
        public IReadOnlyList<FontRun> FontRuns
        {
            get
            {
                Layout();
                return _fontRuns;
            }
        }

        /// <summary>
        /// Get all the lines for this text block
        /// </summary>
        public IReadOnlyList<TextLine> Lines
        {
            get
            {
                Layout();
                return _lines;
            }
        }

        /// <summary>
        /// Paint this text block
        /// </summary>
        /// <param name="canvas">The Skia canvas to paint to</param>
        /// <param name="options">Options controlling the paint operation</param>
        public void Paint(SKCanvas canvas, TextPaintOptions options = null)
        {
            // Ensure have options
            if (options == null)
                options = TextPaintOptions.Default;

            // Ensure layout done
            Layout();

            // Create context
            var ctx = new PaintTextContext()
            {
                Canvas = canvas,
                Options = options,
            };

            // Prepare selection
            if (options.SelectionStart.HasValue && options.SelectionEnd.HasValue)
            {
                ctx.SelectionStart = Math.Min(options.SelectionStart.Value, options.SelectionEnd.Value);
                ctx.SelectionEnd = Math.Max(options.SelectionStart.Value, options.SelectionEnd.Value);
                ctx.PaintSelectionBackground = new SKPaint()
                {
                    Color = options.SelectionColor,
                    IsStroke = false,
                    IsAntialias = options.IsAntialias,
                };
            }
            else
            {
                ctx.SelectionStart = -1;
                ctx.SelectionEnd = -1;
            }

            // Paint each line
            foreach (var l in _lines)
            {
                l.Paint(ctx);
            }

            // Clean up
            ctx.PaintSelectionBackground?.Dispose();
        }

        /// <summary>
        /// Paint this text block
        /// </summary>
        /// <param name="canvas">The Skia canvas to paint to</param>
        /// <param name="position">The top left position within the canvas to draw at</param>
        /// <param name="options">Options controlling the paint operation</param>
        public void Paint(SKCanvas canvas, SKPoint position, TextPaintOptions options = null)
        {
            // Translate
            canvas.Save();
            canvas.Translate(position.X, position.Y);

            // Paint it
            Paint(canvas, options);

            // Restore and done!
            canvas.Restore();
        }
    
        /// <summary>
        /// Get the measured height of the text
        /// </summary>
        public float MeasureHeight
        {
            get
            {
                Layout();
                return _measuredHeight;
            }
        }

        /// <summary>
        /// Get the measured width of the text (excluding required left margin)
        /// </summary>
        public float MeasureWidth
        {
            get
            {
                Layout();
                return _measuredWidth;
            }
        }

        /// <summary>
        /// Returns the inset amount from the left hand side
        /// to any actual drawn content when text is right or center aligned.
        /// </summary>
        public float MeasuredInset
        {
            get
            {
                if (!_maxWidth.HasValue)
                    return 0;

                Layout();

                switch (ResolveTextAlignment())
                {
                    case TextAlignment.Left:
                        return 0;

                    case TextAlignment.Right:
                        return _maxWidthResolved - _measuredWidth;

                    case TextAlignment.Center:
                        return (_maxWidthResolved - _measuredWidth) / 2;
                }

                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// The minimum required left margin to ensure that underhangin glyphs aren't cropped
        /// </summary>
        public float MinLeftMargin
        {
            get
            {
                Layout();
                return _minLeftMargin;
            }
        }

        /// <summary>
        /// Get the required left margin - how much the text actually overhangs the left margin
        /// </summary>
        public float RequiredLeftMargin
        {
            get
            {
                Layout();
                if (!_requiredLeftMargin.HasValue)
                {
                    float required = 0;
                    foreach (var l in _lines)
                    {
                        if (l.Runs.Count == 0)
                            continue;
                        var r = l.Runs[0];
                        if (r.RunKind == FontRunKind.TrailingWhitespace)
                            continue;
                        var m = r.CalculateRequiredLeftMargin();
                        if (m > required)
                            required = m;
                    }
                    _requiredLeftMargin = required;
                }
                return _requiredLeftMargin.Value;
            }
        }

        /// <summary>
        /// Hit test this block of text
        /// </summary>
        /// <param name="x">The x-coordinate relative to top left of the block</param>
        /// <param name="y">The x-coordinate relative to top left of the block</param>
        /// <returns>A HitTestResult</returns>
        public HitTestResult HitTest(float x, float y)
        {
            Layout();

            var htr = new HitTestResult();

            // Work out which line number we're over
            htr.OverLine = -1;
            htr.OverCluster = -1;
            for (int i = 0; i < _lines.Count; i++)
            {
                var l = _lines[i];
                if (y >= l.YPosition && y < l.YPosition + l.Height)
                {
                    htr.OverLine = i;
                }
            }

            // Work out the closest line
            if (htr.OverLine >= 0)
            {
                htr.ClosestLine = htr.OverLine;
            }
            else if (y < 0)
            {
                htr.ClosestLine = 0;
            }
            else
            {
                htr.ClosestLine = _lines.Count - 1;
            }

            // Hit test each cluster
            if (htr.ClosestLine >= 0 && htr.ClosestLine < _lines.Count)
            {
                // Hit test the line
                var l = _lines[htr.ClosestLine];
                l.HitTest(x, ref htr);
            }

            // If we're not over the line, we're also not over the character
            if (htr.OverLine < 0)
                htr.OverCluster = -1;

            System.Diagnostics.Debug.Assert(htr.ClosestCluster >= 0);

            return htr;
        }


        // Build map of all cursor positions
        void BuildCursorIndicies()
        {
            Layout();
            if (_cursorIndicies.Count == 0)
            {
                foreach (var r in _lines.SelectMany(x => x.Runs))
                {
                    for (int i = 0; i < r.Clusters.Length; i++)
                    {
                        _cursorIndicies.Add(r.Clusters[i]);
                    }
                }
                _cursorIndicies.Add(_codePoints.Length);
                _cursorIndicies = _cursorIndicies.OrderBy(x => x).Distinct().ToList();
            }
        }

        /// <summary>
        /// Retrieves a list of all valid cursor positions
        /// </summary>
        public IReadOnlyList<int> CursorIndicies
        {
            get
            {
                BuildCursorIndicies();
                return _cursorIndicies;
            }
        }

        /// <summary>
        /// Given a code point index, find the index in the CursorIndicies
        /// </summary>
        /// <param name="codePointIndex">The code point index to lookup</param>
        /// <returns>The index in the code point idnex in the CursorIndicies array</returns>
        public int LookupCursorIndex(int codePointIndex)
        {
            BuildCursorIndicies();
            int index = _cursorIndicies.BinarySearch(codePointIndex);
            if (index < 0)
                index = ~index;
            return index;
        }

        /// <summary>
        /// Calculates useful information for displaying a cursor
        /// </summary>
        /// <param name="codePointIndex">The code point index of the cursor</param>
        /// <returns>A CursorInfo struct</returns>
        public CaretInfo GetCursorInfo(int codePointIndex)
        {
            // Look up the cursor index
            int cpii = LookupCursorIndex(codePointIndex);

            // Create cursor info
            var ci = new CaretInfo();
            ci.CodePointIndex = _cursorIndicies[cpii];
            ci.NextCodePointIndex = cpii + 1 < _cursorIndicies.Count ? _cursorIndicies[cpii+1] : ci.CodePointIndex;
            ci.PreviousCodePointIndex = cpii > 0 ? _cursorIndicies[cpii - 1] : 0;

            var frIndex = FindFontRunForCodePointIndex(codePointIndex);

            if (frIndex >= 0)
                ci.FontRun = _fontRuns[frIndex];

            if (frIndex > 0)
            {
                if (ci.FontRun.Start == codePointIndex && frIndex > 0)
                {
                    var frPrior = _fontRuns[frIndex - 1];
                    if (frPrior.Direction == TextDirection.RTL && frPrior.End == codePointIndex)
                    {
                        if (frPrior.RunKind != FontRunKind.TrailingWhitespace)
                            ci.FontRun = frPrior;
                    }
                }
            }

            if (ci.FontRun == null)
                ci.FontRun = _fontRuns[_fontRuns.Count - 1];

            return ci;
        }

        /// <summary>
        /// Find the font run holding a code point index
        /// </summary>
        /// <param name="codePointIndex"></param>
        /// <returns></returns>
        public int FindFontRunForCodePointIndex(int codePointIndex)
        {
            // Past end of text?
            if (codePointIndex >= _codePoints.Length)
                return -1;

            // Look up font run
            int frIndex = FontRuns.BinarySearch(codePointIndex, (run, value) =>
            {
                return (run.End - 1) - codePointIndex;
            });
            if (frIndex < 0)
                frIndex = ~frIndex;

            // Return the font run
            var fr = _fontRuns[frIndex];
            System.Diagnostics.Debug.Assert(codePointIndex >= fr.Start);
            System.Diagnostics.Debug.Assert(codePointIndex < fr.End);
            return frIndex;
        }

        /// <summary>
        /// The minimum required right margin to ensure overhanging glyphs aren't cropped
        /// </summary>
        public float MinRightMargin => 0;

        /// <summary>
        /// Invalidate the layout
        /// </summary>
        void InvalidateLayout()
        {
            _needsLayout = true;
        }

        /// <summary>
        /// Set if the current layout is dirty
        /// </summary>
        bool _needsLayout = true;

        /// <summary>
        /// Maximum width (wrap point, or null for no wrapping)
        /// </summary>
        float? _maxWidth;

        /// <summary>
        /// Width at which to wrap content
        /// </summary>
        float _maxWidthResolved = float.MaxValue;

        /// <summary>
        /// Maximum height (crop lines after this)
        /// </summary>
        float? _maxHeight;

        /// <summary>
        /// Maximum layout height
        /// </summary>
        float _maxHeightResolved = float.MaxValue;

        /// <summary>
        /// Maximum number of lines
        /// </summary>
        int? _maxLines;

        /// <summary>
        /// Maximum number of lines
        /// </summary>
        int _maxLinesResolved = int.MaxValue;

        /// <summary>
        /// Text alignment
        /// </summary>
        TextAlignment _textAlignment = TextAlignment.Left;

        /// <summary>
        /// Base direction
        /// </summary>
        TextDirection _baseDirection = TextDirection.LTR;

        /// <summary>
        /// All code points as supplied by user, accumulated into a single buffer
        /// </summary>
        Utf32Buffer _codePoints = new Utf32Buffer();

        /// <summary>
        /// Reusable buffer for bidi data
        /// </summary>
        BidiData bidiData = new BidiData();

        /// <summary>
        /// A list of styled runs, as supplied by user
        /// </summary>
        List<StyledRun> _styledRuns = new List<StyledRun>();

        /// <summary>
        /// A list of font runs, after splitting by directionality, user styles and font fallback
        /// </summary>
        List<FontRun> _fontRuns = new List<FontRun>();

        /// <summary>
        /// Helper for splitting code into linebreaks
        /// </summary>
        LineBreaker _lineBreaker = new LineBreaker();

        /// <summary>
        /// The measured height
        /// </summary>
        float _measuredHeight;

        /// <summary>
        /// The measured width
        /// </summary>
        float _measuredWidth;

        /// <summary>
        /// The minimum left margin
        /// </summary>
        float _minLeftMargin;

        /// <summary>
        /// The required left margin
        /// </summary>
        float? _requiredLeftMargin = null;

        /// <summary>
        /// The final laid out set of lines
        /// </summary>
        List<TextLine> _lines = new List<TextLine>();

        /// <summary>
        /// True to use MS Word style RTL layout
        /// </summary>
        bool _useMSWordStyleRtlLayout = false;

        /// <summary>
        /// Calculate cursor positions
        /// </summary>
        List<int> _cursorIndicies = new List<int>();

        /// <summary>
        /// Resolve the text alignment when set to Auto
        /// </summary>
        /// <returns>Resolved text alignment (left, right or center)</returns>
        TextAlignment ResolveTextAlignment()
        {
            if (_textAlignment == TextAlignment.Auto)
                return BaseDirection == TextDirection.LTR ? TextAlignment.Left : TextAlignment.Right;
            else
                return _textAlignment;
        }
        
        /// <summary>
        /// Split into runs based on directionality and style switch points
        /// </summary>
        void BuildFontRuns()
        {
            byte paragraphEmbeddingLevel = 0;

            if (BaseDirection == TextDirection.RTL && !_useMSWordStyleRtlLayout)
            {
                paragraphEmbeddingLevel = 1;
            }

            // Break supplied text into directionality runs
            bidiData.Init(_codePoints.AsSlice(), paragraphEmbeddingLevel);
            var bidiRuns = new Bidi(bidiData).Runs.ToList();

            // Split...
            var pos = 0;
            int bidiRun = 0;
            int styleRun = 0;
            while (pos < _codePoints.Length)
            {
                // Move to next bidi/style run
                if (pos == bidiRuns[bidiRun].End)
                    bidiRun++;
                if (pos == _styledRuns[styleRun].End)
                    styleRun++;

                // Work out where this run ends
                int nextPos = Math.Min(bidiRuns[bidiRun].End, _styledRuns[styleRun].End);

                // Add the run
                var dir = bidiRuns[bidiRun].Direction == Directionality.L ? TextDirection.LTR : TextDirection.RTL;
                AddDirectionalRun(_styledRuns[styleRun], pos, nextPos - pos, dir, _styledRuns[styleRun].Style);

                // Move to next position
                pos = nextPos;
            }

            System.Diagnostics.Debug.Assert(bidiRun == bidiRuns.Count - 1);
            System.Diagnostics.Debug.Assert(styleRun == _styledRuns.Count - 1);

            // Add the final run
            var dir2 = bidiRuns[bidiRun].Direction == Directionality.L ? TextDirection.LTR : TextDirection.RTL;
            AddDirectionalRun(_styledRuns[_styledRuns.Count-1], pos, _codePoints.Length - pos, dir2, _styledRuns[styleRun].Style);
        }

        /// <summary>
        /// Gets the Skia type face for a IStyle
        /// </summary>
        /// <param name="style">The style</param>
        /// <param name="ignoreFontVariants">When true, doesn't embolden super/sub scripts</param>
        /// <returns>The Skia typeface</returns>
        SKTypeface TypefaceFromStyle(IStyle style, bool ignoreFontVariants = false)
        {
            // Extra weight for superscript/subscript
            int extraWeight = 0;
            if (!ignoreFontVariants && (style.FontVariant == FontVariant.SuperScript || style.FontVariant == FontVariant.SubScript))
            {
                extraWeight += 100;
            }

            // Get the typeface
            return SKTypeface.FromFamilyName(
                style.FontFamily, 
                (SKFontStyleWeight)(style.FontWeight + extraWeight), 
                0, 
                style.FontItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright
                );
        }

        /// <summary>
        /// Adds a run of directional text
        /// </summary>
        /// <param name="styledRun">The styled run the directional run was created from</param>
        /// <param name="start">Index of the first code point _codePoints buffer</param>
        /// <param name="length">Number of code points in this run</param>
        /// <param name="direction">The direction of the text</param>
        /// <param name="style">The user supplied style for this run</param>
        void AddDirectionalRun(StyledRun styledRun, int start, int length, TextDirection direction, IStyle style)
        {
            // Quit if redundant...
            if (length == 0)
                return;

            // Get the typeface
            var typeface = TypefaceFromStyle(style);

            // Get the slice of code points
            var codePointsSlice = _codePoints.SubSlice(start, length);

            // Split into font fallback runs
            foreach (var fontRun in FontFallback.GetFontRuns(codePointsSlice, typeface))
            {
                // Add this run
                AddFontRun(styledRun, start + fontRun.Start, fontRun.Length, direction, style, fontRun.Typeface);
            }
        }

        /// <summary>
        /// Adds a run of single font text
        /// </summary>
        /// <param name="styledRun">The styled run the directional run was created from</param>
        /// <param name="start">Index of the first code point _codePoints buffer</param>
        /// <param name="length">Number of code points in this run</param>
        /// <param name="direction">The direction of the text</param>
        /// <param name="style">The user supplied style for this run</param>
        /// <param name="typeface">The typeface of the run</param>
        void AddFontRun(StyledRun styledRun, int start, int length, TextDirection direction, IStyle style, SKTypeface typeface)
        {
            // Get code points slice
            var codePoints = _codePoints.SubSlice(start, length);

            // Add the font face run
            _fontRuns.Add(CreateFontRun(styledRun, codePoints, direction, style, typeface));
        }

        /// <summary>
        /// Helper to create a font run
        /// </summary>
        /// <param name="styledRun">The user styled run owning this font run</param>
        /// <param name="codePoints">The code points of the run</param>
        /// <param name="direction">The run direction</param>
        /// <param name="style">The user supplied style for this run</param>
        /// <param name="typeface">The typeface of the run</param>
        /// <returns>A FontRun</returns>
        FontRun CreateFontRun(StyledRun styledRun, Slice<int> codePoints, TextDirection direction, IStyle style, SKTypeface typeface)
        {
            // Shape the text
            var shaper = TextShaper.ForTypeface(typeface);
            var shaped = shaper.Shape(codePoints, style, direction, codePoints.Start);

            // Update minimum required left margin
            if (shaped.XMin < 0 && -shaped.XMin > _minLeftMargin)
            {
                _minLeftMargin = -shaped.XMin;
            }

            // Create the run
            return new FontRun()
            {
                StyledRun = styledRun,
                CodePointBuffer = _codePoints,
                Start = codePoints.Start,
                Length = codePoints.Length,
                Style = style,
                Direction = direction,
                Typeface = typeface,
                Glyphs = new Slice<ushort>(shaped.GlyphIndicies),
                GlyphPositions = new Slice<SKPoint>(shaped.Points),
                RelativeCodePointXCoords = new Slice<float>(shaped.CodePointXCoords),
                Clusters = new Slice<int>(shaped.Clusters),
                Ascent = shaped.Ascent,
                Descent = shaped.Descent,
                Width = shaped.EndXCoord.X,
            };        
        }

        /// <summary>
        /// Break the list of font runs into lines
        /// </summary>
        void BreakLines()
        {
            // Work out possible line break positions
            _lineBreaker.Reset(_codePoints.AsSlice());
            var lineBreakPositions = _lineBreaker.GetBreaks();

            int frIndexStartOfLine = 0;     // Index of the first font run in the current line
            int frIndex = 0;                // Index of the current font run
            int lbrIndex = 0;               // Index of the next unconsumed linebreak
            float consumedWidth = 0;        // Total width of all font runs placed on this line
            int frSplitIndex = -1;          // Index of the last font run that contained fitting break point
            int codePointIndexSplit = -1;   // Code point index of the last fitting break point measure position
            int codePointIndexWrap = -1;    // Code point index of the last fitting breaj point wrap position

            while (frIndex < _fontRuns.Count)
            {
                // Get the font run, update it's position
                // and move to next
                var fr = _fontRuns[frIndex];
                fr.XCoord = consumedWidth;
                consumedWidth += fr.Width;

                // Skip line breaks
                bool breakLine = false;
                while (lbrIndex < lineBreakPositions.Count)
                {
                    // Past this run?
                    var lbr = lineBreakPositions[lbrIndex];
                    if (lbr.PositionMeasure < fr.Start)
                    {
                        lbrIndex++;
                        continue;
                    }
                    if (lbr.PositionMeasure >= fr.End)
                        break;

                    // Do we need to break
                    var totalWidthToThisBreakPoint = fr.XCoord + fr.LeadingWidth(lbr.PositionMeasure);
                    if (totalWidthToThisBreakPoint > _maxWidthResolved)
                    {
                        breakLine = true;
                        break;
                    }

                    // It fits, remember that
                    lbrIndex++;
                    frSplitIndex = frIndex;
                    codePointIndexSplit = lbr.PositionMeasure;
                    codePointIndexWrap = lbr.PositionWrap;

                    if (lbr.Required)
                    {
                        breakLine = true;
                        break;
                    }
                }

                // If we're on the last run and we've exceeded the width limit then force a break
                if (!breakLine && frIndex + 1 == _fontRuns.Count && consumedWidth > _maxWidthResolved)
                    breakLine = true;

                // Break the line here?
                if (!breakLine)
                {
                    frIndex++;
                    continue;
                }

                // If there wasn't a line break anywhere in the line, then we need to force one
                // on a character boundary
                if (frSplitIndex < 0)
                {
                    // Get the last run that partially fitted
                    frIndex = frIndexStartOfLine;
                    fr = _fontRuns[frIndex];
                    var room = _maxWidthResolved - fr.XCoord;
                    frSplitIndex = frIndex;
                    codePointIndexSplit = fr.FindBreakPosition(room, frSplitIndex == frIndexStartOfLine);
                    codePointIndexWrap = codePointIndexSplit;
                    while (codePointIndexWrap < _codePoints.Length && UnicodeClasses.LineBreakClass(_codePoints[codePointIndexWrap]) == LineBreakClass.SP)
                        codePointIndexWrap++;
                }

                // Split it
                fr = _fontRuns[frSplitIndex];
                if (codePointIndexSplit == fr.Start)
                {
                    // Split exactly before the run
                }
                else if (codePointIndexSplit == fr.End)
                {
                    // Split exactly after the run
                    frSplitIndex++;
                }
                else
                {
                    // Split in the middle of the run
                    frSplitIndex++;
                    _fontRuns.Insert(frSplitIndex, fr.Split(codePointIndexSplit));
                }

                // Trailing whitespace 
                int frTrailingWhiteSpaceIndex = frSplitIndex;
                while (frTrailingWhiteSpaceIndex < _fontRuns.Count && _fontRuns[frTrailingWhiteSpaceIndex].Start < codePointIndexWrap)
                {
                    if (codePointIndexWrap < _fontRuns[frTrailingWhiteSpaceIndex].End)
                    {
                        var fr2 = _fontRuns[frTrailingWhiteSpaceIndex].Split(codePointIndexWrap);
                        _fontRuns.Insert(frTrailingWhiteSpaceIndex + 1, fr2);
                    }

                    frTrailingWhiteSpaceIndex++;
                }

                // Build the final line
                BuildLine(frIndexStartOfLine, frSplitIndex, frTrailingWhiteSpaceIndex);

                // Reset for the next line
                frSplitIndex = -1;
                frIndex = frTrailingWhiteSpaceIndex;
                frIndexStartOfLine = frIndex;
                consumedWidth = 0;

                // Check height constraints and quit if finished
                if (!CheckHeightConstraints())
                    return;
            }

            // Build the final line
            if (frIndexStartOfLine < _fontRuns.Count)
            {
                BuildLine(frIndexStartOfLine, _fontRuns.Count, _fontRuns.Count);
                CheckHeightConstraints();
            }
        }

        /// <summary>
        /// Construct a single line from the specified font run indicies
        /// </summary>
        /// <param name="frIndexStartOfLine">Index of the first font run in the line</param>
        /// <param name="frSplitIndex">Index of the last type</param>
        /// <param name="frTrailingWhiteSpaceIndex"></param>
        void BuildLine(int frIndexStartOfLine, int frSplitIndex, int frTrailingWhiteSpaceIndex)
        {
            // Create the line
            var line = new TextLine();
            line.TextBlock = this;
            line.YPosition = _measuredHeight;

            // Add runs
            for (int i = frIndexStartOfLine; i < frTrailingWhiteSpaceIndex; i++)
            {
                // Tag trailing whitespace appropriately
                if (i >= frSplitIndex)
                {
                    var fr = _fontRuns[i];
                    fr.RunKind = FontRunKind.TrailingWhitespace;
                    if (fr.Direction != BaseDirection)
                    {
                        // Create a new font run over the same text span but using the base direction
                        _fontRuns[i] = CreateFontRun(fr.StyledRun, fr.CodePoints, BaseDirection, fr.Style, fr.Typeface);
                    }
                }

                line.RunsInternal.Add(_fontRuns[i]);
            }

            // Add the line to the collection
            _lines.Add(line);

            // Lay it out
            LayoutLine(line);

            // Update y position
            _measuredHeight += line.Height;
        }

        /// <summary>
        /// Layout the font runs within a line
        /// </summary>
        /// <param name="line">The line to layout</param>
        void LayoutLine(TextLine line)
        {
            // How much to x-adjust all runs by to position correctly
            float xAdjust;

            // Which direction?
            if (BaseDirection == TextDirection.LTR)
            {
                xAdjust = LayoutLineLTR(line);
            }
            else
            {
                if (_useMSWordStyleRtlLayout)
                {
                    xAdjust = LayoutLineRTLWordStyle(line);
                }
                else
                {
                    xAdjust = LayoutLineRTL(line);
                }
            }

            // Process all runs, setting their final left-aligned x-position and 
            // calculating metrics for the line as a whole
            float maxAbove = 0;
            float maxBelow = 0;
            float maxAscent = 0;
            float maxDescent = 0;
            for (int frIndex = 0; frIndex < line.Runs.Count; frIndex++)
            {
                // Get the run
                var fr = line.Runs[frIndex];

                // Adjust run position
                fr.XCoord += xAdjust;

                var above = -fr.Ascent + fr.HalfLeading;
                var below = fr.Descent + fr.HalfLeading;

                if (above > maxAbove)
                    maxAbove = above;
                if (below > maxBelow)
                    maxBelow = below;

                if (fr.Ascent < maxAscent)
                    maxAscent = fr.Ascent;
                if (fr.Descent > maxDescent)
                    maxDescent = fr.Descent;
            }

            // Store line metrics
            line.MaxAscent = maxAscent;
            line.MaxDescent = maxDescent;
            line.Height = maxAbove + maxBelow;
            line.BaseLine = maxAbove;
        }

        /// <summary>
        /// Layout a line (LTR edition)
        /// </summary>
        /// <param name="line">The line to be laid out</param>
        /// <returns>A final x-adjustment to be applied to the line's font runs</returns>
        private float LayoutLineLTR(TextLine line)
        {
            float x = 0;
            float trailingWhitespaceWidth = 0;
            for (int i = 0; i < line.Runs.Count; i++)
            {
                var fr = line.Runs[i];
                if (fr.Direction == TextDirection.LTR)
                {
                    fr.XCoord = x;
                    x += fr.Width;

                    if (fr.RunKind == FontRunKind.TrailingWhitespace)
                        trailingWhitespaceWidth += fr.Width;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(fr.RunKind == FontRunKind.Normal);

                    int j = i;
                    while (j + 1 < line.Runs.Count && line.Runs[j + 1].Direction == TextDirection.RTL)
                        j++;

                    int continueFrom = j;

                    while (j >= i)
                    {
                        fr = line.Runs[j];
                        fr.XCoord = x;
                        x += fr.Width;
                        j--;
                    }

                    i = continueFrom;
                }
            }

            // Store content width
            line.Width = x - trailingWhitespaceWidth;

            return 0;
        }

        /// <summary>
        /// Layout a line (RTL edition)
        /// </summary>
        /// <param name="line">The line to be laid out</param>
        /// <returns>A final x-adjustment to be applied to the line's font runs</returns>
        private float LayoutLineRTL(TextLine line)
        {
            float x = 0;
            float trailingWhitespaceWidth = 0;
            for (int i = 0; i < line.Runs.Count; i++)
            {
                var fr = line.Runs[i];
                if (fr.Direction == TextDirection.RTL)
                {
                    x -= fr.Width;
                    fr.XCoord = x;

                    if (fr.RunKind == FontRunKind.TrailingWhitespace)
                        trailingWhitespaceWidth += fr.Width;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(fr.RunKind == FontRunKind.Normal);

                    int j = i;
                    while (j + 1 < line.Runs.Count && line.Runs[j + 1].Direction == TextDirection.LTR)
                        j++;

                    int continueFrom = j;

                    while (j >= i)
                    {
                        fr = line.Runs[j];
                        x -= fr.Width;
                        fr.XCoord = x;
                        j--;
                    }

                    i = continueFrom;
                }
            }

            // Work out final width
            float totalWidth = -x - trailingWhitespaceWidth;

            // Store content width
            line.Width = totalWidth;

            // Everything is currently laid out from 0 to the left, so we 
            // need to shift right to make it left aligned
            return totalWidth;
        }

        /// <summary>
        /// Layout a line (MS Word RTL edition)
        /// </summary>
        /// <param name="line">The line to be laid out</param>
        /// <returns>A final x-adjustment to be applied to the line's font runs</returns>
        private float LayoutLineRTLWordStyle(TextLine line)
        {
            float xPos = 0;
            float totalWidth = 0;
            float trailingWhitespaceWidth = 0;
            for (int i = 0; i < line.Runs.Count; i++)
            {
                // Get the direction of this group
                var groupDirection = line.Runs[i].Direction;

                // Count the number of runs with the same directionality
                int j = i;
                float groupWidth = 0;
                while (j < line.Runs.Count && line.Runs[j].Direction == groupDirection)
                {
                    if (line.Runs[i].RunKind == FontRunKind.TrailingWhitespace)
                        trailingWhitespaceWidth += line.Runs[i].Width;

                    groupWidth += line.Runs[j].Width;
                    j++;
                }

                totalWidth += groupWidth;

                // How many runs in the group
                var runsInGroup = j - i;

                // Place the runs in the group
                if (groupDirection == TextDirection.LTR)
                {
                    // Left to right group
                    for (; i < j; i++)
                    {
                        line.Runs[i].XCoord = xPos;
                        xPos += line.Runs[i].Width;
                    }
                }
                else
                {
                    // Right to left group
                    xPos += groupWidth;
                    var x = xPos;
                    for (; i < j; i++)
                    {
                        x -= line.Runs[i].Width;
                        line.Runs[i].XCoord = x;
                    }
                }

                i = j - 1;
            }

            line.Width = xPos - trailingWhitespaceWidth;

            return _maxWidthResolved - (xPos - trailingWhitespaceWidth);
        }

        /// <summary>
        /// Finalize lines, positioning for horizontal alignment and 
        /// moving all glyphs into position (relative to the text block top left)
        /// </summary>
        void FinalizeLines()
        {
            // Work out the measured width
            foreach (var l in _lines)
            {
                if (l.Width > _measuredWidth)
                    _measuredWidth = l.Width;
            }

            // Finalize lines
            var ta = ResolveTextAlignment();
            foreach (var line in _lines)
            {
                // Work out x-alignement adjust for this line
                float xAdjust = 0;
                switch (ta)
                {
                    case TextAlignment.Right:
                        xAdjust = (_maxWidth ?? _measuredWidth) - line.Width;
                        break;

                    case TextAlignment.Center:
                        xAdjust = ((_maxWidth ?? _measuredWidth) - line.Width) / 2;
                        break;
                }

                // Position each run
                for (int frIndex = 0; frIndex < line.Runs.Count; frIndex++)
                {
                    var fr = line.Runs[frIndex];
                    fr.Line = line;
                    fr.XCoord += xAdjust;
                    fr.MoveGlyphs(fr.XCoord, line.YPosition + line.BaseLine);
                }
            }
        }

        /// <summary>
        /// Finds the font run containing a specified code point index
        /// </summary>
        /// <param name="from">The font run index to start from</param>
        /// <param name="codePointIndex">The code point index being searched for</param>
        /// <returns>The index of the font run containing the specified code point 
        /// index, or font run count if past the end</returns>
        int FindFontRunForCodePointIndex(int from, int codePointIndex)
        {
            int frIndex = from;
            while (true)
            {
                var fr = _fontRuns[frIndex];
                if (codePointIndex < fr.Start)
                {
                    System.Diagnostics.Debug.Assert(frIndex > 0);
                    frIndex--;
                }
                else if (codePointIndex >= fr.End)
                {
                    frIndex++;
                    if (frIndex == _fontRuns.Count)
                    {
                        System.Diagnostics.Debug.Assert(codePointIndex == _codePoints.Length);
                        return _codePoints.Length;
                    }
                }
                else
                {
                    return frIndex;
                }
            }
        }

        /// <summary>
        /// Re-usable buffer holding just the ellipsis character
        /// </summary>
        static Utf32Buffer ellipsis = new Utf32Buffer("…");

        /// <summary>
        /// Create a special font run containing the ellipsis character
        /// based on an existing run
        /// </summary>
        /// <param name="basedOn">The run to base the styling on</param>
        /// <returns>A new font run containing the ellipsis character</returns>
        FontRun CreateEllipsisRun(FontRun basedOn)
        {
            // Get the type face
            var typeface = TypefaceFromStyle(basedOn.Style, true);

            // Split into font fallback runs (there should only ever be just one)
            var fontRun = FontFallback.GetFontRuns(ellipsis.AsSlice(), typeface).Single();

            // Create the new run and mark is as a special run type for ellipsis
            var fr = CreateFontRun(basedOn.StyledRun, ellipsis.SubSlice(fontRun.Start, fontRun.Length), BaseDirection, basedOn.Style, fontRun.Typeface);
            fr.RunKind = FontRunKind.Ellipsis;

            // Done
            return fr;
        }

        /// <summary>
        /// Update the current line with a new font run containing the trailing ellipsis
        /// </summary>
        /// <param name="line">The line to be updated</param>
        void AdornLineWithEllipsis(TextLine line)
        {
            var lastRun = line.Runs[line.Runs.Count - 1];

            // Remove all trailing whitespace from the line
            for (int i = line.Runs.Count - 1; i >= 0; i--)
            {
                if (line.Runs[i].RunKind == FontRunKind.TrailingWhitespace)
                    line.RunsInternal.RemoveAt(i);
            }

            // Calculate the total width of the line
            float totalWidth = 0;
            for (int i = 0; i < line.Runs.Count; i++)
            {
                totalWidth += line.Runs[i].Width;
            }

            // Get the new last run (if any)
            if (line.Runs.Count> 0)
                lastRun = line.Runs[line.Runs.Count - 1];

            // Create a new run for the ellipsis
            var ellipsisRun = CreateEllipsisRun(lastRun);

            // Is there enough room in the line
            var removeWidth = totalWidth + ellipsisRun.Width - _maxWidthResolved;
            if (removeWidth > 0)
            {
                for (int i = line.Runs.Count-1; i>=0; i--)
                {
                    var fr = line.Runs[i];

                    // Does this run have enough to remove?
                    // No, remove it all
                    if (fr.Width < removeWidth)
                    {
                        removeWidth -= fr.Width;
                        line.RunsInternal.RemoveAt(i);
                        continue;
                    }

                    // Work out where to split this run
                    int pos = fr.FindBreakPosition(fr.Width - removeWidth, false);
                    if (pos == fr.Start)
                    {
                        // Nothing fits, remove it all
                        line.RunsInternal.RemoveAt(i);
                    }
                    else
                    {
                        // Split it
                        var remaining = fr.Split(pos);

                        // Keep the remaining part in case we need it later (not sure why,
                        // but seems wise).
                        _fontRuns.Insert(_fontRuns.IndexOf(fr) + 1, remaining);
                    }

                    break;
                }
            }

            // Add it to the line
            line.RunsInternal.Add(ellipsisRun);

            // Remember old line height
            var oldHeight = line.Height;
            _measuredHeight -= line.Height;

            // Layout the line again
            LayoutLine(line);

            // Adjust height just in case it changed
            _measuredHeight += line.Height;
        }

        /// <summary>
        /// Check if the current layout has exceeded any height restrictions
        /// and if so, remove any offending lines and optionally create the
        /// ellipsis at the end indicating the text has been truncated
        /// </summary>
        /// <returns>True if can continue adding lines; otherwise false</returns>
        bool CheckHeightConstraints()
        {
            // Have we exceeded the height limit
            if (_measuredHeight > _maxHeightResolved)
            {
                // Remove the last line (unless it's the only line)
                if (_lines.Count > 1)
                {
                    _measuredHeight -= _lines[_lines.Count - 1].Height;
                    _lines.RemoveAt(_lines.Count - 1);
                }
                AdornLineWithEllipsis(_lines[_lines.Count-1]);
                return false;
            }

            // Have we hit the line count limit?
            if (_lines.Count >= _maxLinesResolved)
            {
                AdornLineWithEllipsis(_lines[_lines.Count-1]);
                return false;
            }

            return true;
        }

    }
}
