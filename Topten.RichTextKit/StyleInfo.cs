using SkiaSharp;

namespace Topten.RichTextKit
{
    public struct StyleInfo : IStyle
    {
        public StyleInfo(IStyle initial)
        {
            FontFamily = initial.FontFamily;
            FontSize = initial.FontSize;
            FontWeight = initial.FontWeight;
            FontWidth = initial.FontWidth;
            FontItalic = initial.FontItalic;
            Underline = initial.Underline;
            StrikeThrough = initial.StrikeThrough;
            LineHeight = initial.LineHeight;
            TextColor = initial.TextColor;
            BackgroundColor = initial.BackgroundColor;
            HaloColor = initial.HaloColor;
            HaloWidth = initial.HaloWidth;
            HaloBlur = initial.HaloBlur;
            LetterSpacing = initial.LetterSpacing;
            FontVariant = initial.FontVariant;
            TextDirection = initial.TextDirection;
            ReplacementCharacter = initial.ReplacementCharacter;
        }

        public string FontFamily { get; internal set; }

        public float? FontSize { get; internal set; }

        public int? FontWeight { get; internal set; }

        public SKFontStyleWidth? FontWidth { get; internal set; }

        public bool? FontItalic { get; internal set; }

        public UnderlineStyle? Underline { get; internal set; }

        public StrikeThroughStyle? StrikeThrough { get; internal set; }

        public float? LineHeight { get; internal set; }

        public SKColor? TextColor { get; internal set; }

        public SKColor? BackgroundColor { get; internal set; }

        public SKColor? HaloColor { get; internal set; }

        public float? HaloWidth { get; internal set; }

        public float? HaloBlur { get; internal set; }

        public float? LetterSpacing { get; internal set; }

        public FontVariant? FontVariant { get; internal set; }

        public TextDirection? TextDirection { get; internal set; }

        public char? ReplacementCharacter { get; internal set; }

        public StyleInfo Union(IStyle otherStyle)
        {
            return new StyleInfo()
            {
                BackgroundColor = BackgroundColor != otherStyle.BackgroundColor ? null : BackgroundColor,
                FontFamily = FontFamily != otherStyle.FontFamily ? null : FontFamily,
                FontWeight = FontWeight != otherStyle.FontWeight ? null : FontWeight,
                FontItalic = FontItalic != otherStyle.FontItalic ? null : FontItalic,
                FontSize = FontSize != otherStyle.FontSize ? null : FontSize,
                FontVariant = FontVariant != otherStyle.FontVariant ? null : FontVariant,
                FontWidth = FontWidth != otherStyle.FontWidth ? null : FontWidth,
                HaloBlur = HaloBlur != otherStyle.HaloBlur ? null : HaloBlur,
                HaloColor = HaloColor != otherStyle.HaloColor ? null : HaloColor,
                HaloWidth = HaloWidth != otherStyle.HaloWidth ? null : HaloWidth,
                LetterSpacing = LetterSpacing != otherStyle.LetterSpacing ? null : LetterSpacing,
                LineHeight = LineHeight != otherStyle.LineHeight ? null : LineHeight,
                ReplacementCharacter = ReplacementCharacter != otherStyle.ReplacementCharacter ? null : ReplacementCharacter,
                TextDirection = TextDirection != otherStyle.TextDirection ? null : TextDirection,
                Underline = Underline != otherStyle.Underline ? null : Underline,
                StrikeThrough = StrikeThrough != otherStyle.StrikeThrough ? null : StrikeThrough,
                TextColor = TextColor != otherStyle.TextColor ? null : TextColor
            };
        }

        public StyleInfo Difference(IStyle otherStyle)
        {
            return new StyleInfo()
            {
                BackgroundColor = BackgroundColor == otherStyle.BackgroundColor ? null : otherStyle.BackgroundColor,
                FontFamily = FontFamily == otherStyle.FontFamily ? null : otherStyle.FontFamily,
                FontWeight = FontWeight == otherStyle.FontWeight ? null : otherStyle.FontWeight,
                FontItalic = FontItalic == otherStyle.FontItalic ? null : otherStyle.FontItalic,
                FontSize = FontSize == otherStyle.FontSize ? null : otherStyle.FontSize,
                FontVariant = FontVariant == otherStyle.FontVariant ? null : otherStyle.FontVariant,
                FontWidth = FontWidth == otherStyle.FontWidth ? null : otherStyle.FontWidth,
                HaloBlur = HaloBlur == otherStyle.HaloBlur ? null : otherStyle.HaloBlur,
                HaloColor = HaloColor == otherStyle.HaloColor ? null : otherStyle.HaloColor,
                HaloWidth = HaloWidth == otherStyle.HaloWidth ? null : otherStyle.HaloWidth,
                LetterSpacing = LetterSpacing == otherStyle.LetterSpacing ? null : otherStyle.LetterSpacing,
                LineHeight = LineHeight == otherStyle.LineHeight ? null : otherStyle.LineHeight,
                ReplacementCharacter = ReplacementCharacter == otherStyle.ReplacementCharacter ? null : otherStyle.ReplacementCharacter,
                TextDirection = TextDirection == otherStyle.TextDirection ? null : otherStyle.TextDirection,
                Underline = Underline == otherStyle.Underline ? null : otherStyle.Underline,
                StrikeThrough = StrikeThrough == otherStyle.StrikeThrough ? null : otherStyle.StrikeThrough,
                TextColor = TextColor == otherStyle.TextColor ? null : otherStyle.TextColor
            };
        }

        public bool HasProperties()
        {
            return BackgroundColor.HasValue ||
                FontFamily != null ||
                FontWeight.HasValue ||
                FontItalic.HasValue ||
                FontSize.HasValue ||
                FontVariant.HasValue ||
                FontWidth.HasValue ||
                HaloBlur.HasValue ||
                HaloColor.HasValue ||
                HaloWidth.HasValue ||
                LetterSpacing.HasValue ||
                LineHeight.HasValue ||
                ReplacementCharacter.HasValue ||
                TextDirection.HasValue ||
                Underline.HasValue ||
                StrikeThrough.HasValue ||
                TextColor.HasValue;
        }
    }
}
