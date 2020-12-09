using Topten.RichTextKit.Utils;

namespace Topten.RichTextKit.Editor.UndoUnits
{
    class UndoInsertText : UndoUnit<TextDocument>
    {
        public UndoInsertText(TextBlock textBlock, int offset, StyledText text)
        {
            _textBlock = textBlock;
            _offset = offset;
            _length = text.Length;
            _text = text;
        }

        public TextBlock TextBlock => _textBlock;
        public int Offset => _offset;
        public int Length => _length;

        public bool ShouldAppend(StyledText text)
        {
            // If this is a word boundary then don't extend this unit
            return !WordBoundaryAlgorithm.IsWordBoundary(_textBlock.CodePoints.SubSlice(0, _offset + _length), text.CodePoints.AsSlice());
        }

        public void Append(StyledText text)
        {
            // Insert into the text block
            _textBlock.InsertText(_offset + _length, text);

            // Update length
            _length += text.Length;
        }

        public void Replace(StyledText text)
        {
            // Insert into the text block
            _textBlock.DeleteText(_offset, _length);
            _textBlock.InsertText(_offset, text);

            // Update length
            _length = text.Length;
        }

        public override void Do(TextDocument context)
        {
            // Insert the text into the text block
            _textBlock.InsertText(_offset, _text);

            // Release our copy of the text
            _text = null;
        }

        public override void Undo(TextDocument context)
        {
            // Save a copy of the text being deleted
            _text = _textBlock.Extract(_offset, _length);

            // Delete it
            _textBlock.DeleteText(_offset, _length);
        }

        TextBlock _textBlock;
        int _offset;
        int _length;
        StyledText _text;
    }
}
