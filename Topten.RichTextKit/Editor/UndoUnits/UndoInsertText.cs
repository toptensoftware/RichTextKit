using Topten.RichTextKit.Utils;

namespace Topten.RichTextKit.Editor.UndoUnits
{
    class UndoInsertText : UndoUnit<TextDocument>
    {
        public UndoInsertText(TextBlock textBlock, int offset, Slice<int> codePoints)
        {
            _textBlock = textBlock;
            _offset = offset;
            _length = codePoints.Length;
            _codePoints = codePoints;
        }

        public bool Append(Slice<int> codePoints)
        {
            // If this is a word boundary then don't extend this unit
            if (WordBoundaryAlgorithm.IsWordBoundary(_textBlock.CodePoints.SubSlice(0, _offset + _length), codePoints))
                return false;

            // Insert into the text block
            _textBlock.InsertText(_offset + _length, codePoints);

            // Update length
            _length += codePoints.Length;

            return true;
        }

        public override void Do(TextDocument context)
        {
            // Insert the text into the text block
            _textBlock.InsertText(_offset, _codePoints);

            // Release our copy of the text
            _codePoints = Slice<int>.Empty;
        }

        public override void Undo(TextDocument context)
        {
            // Save a copy of the text being deleted
            _codePoints = _textBlock.CodePoints.SubSlice(_offset, _length).Copy();

            // Delete it
            _textBlock.DeleteText(_offset, _length);
        }

        TextBlock _textBlock;
        int _offset;
        int _length;
        Slice<int> _codePoints;
    }
}
