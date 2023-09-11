namespace Topten.RichTextKit
{
    public readonly struct SelectionInfo
    {
        public StyleInfo StyleInfo { get; }
        public TextAlignment? ParagraphAlignment { get; }
        public float? LineSpacing { get; }

        public SelectionInfo(StyleInfo styleInfo, TextAlignment? paragraphAlignment, float? lineSpacing)
        {
            StyleInfo = styleInfo;
            ParagraphAlignment = paragraphAlignment;
            LineSpacing = lineSpacing;
        }
    }
}
