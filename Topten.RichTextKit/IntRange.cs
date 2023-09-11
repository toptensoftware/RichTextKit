namespace Topten.RichTextKit
{
    internal struct IntRange
    {
        internal int Start { get; }
        internal int End { get; }

        internal IntRange(int start, int end)
        {
            Start = start;
            End = end;
        }
    }
}
