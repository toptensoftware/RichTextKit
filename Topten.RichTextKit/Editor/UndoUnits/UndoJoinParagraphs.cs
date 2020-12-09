using Topten.RichTextKit.Utils;

namespace Topten.RichTextKit.Editor.UndoUnits
{
    class UndoJoinParagraphs : UndoUnit<TextDocument>
    {
        public UndoJoinParagraphs(int paragraphIndex)
        {
            _paragraphIndex = paragraphIndex;
        }

        public override void Do(TextDocument context)
        {
            var firstPara = context._paragraphs[_paragraphIndex];
            var secondPara = context._paragraphs[_paragraphIndex + 1];

            // Remember what we need to undo
            _splitPoint = firstPara.Length;
            _removedParagraph = secondPara;

            // Copy all text from the second paragraph
            firstPara.TextBlock.AddText(secondPara.TextBlock);

            // Remove the joined paragraph
            context._paragraphs.RemoveAt(_paragraphIndex + 1);
        }

        public override void Undo(TextDocument context)
        {
            // Delete the joined text from the first paragraph
            var firstPara = context._paragraphs[_paragraphIndex];
            firstPara.TextBlock.DeleteText(_splitPoint, firstPara.TextBlock.Length - _splitPoint);

            // Restore the split paragraph
            context._paragraphs.Insert(_paragraphIndex + 1, _removedParagraph);
        }

        int _paragraphIndex;
        int _splitPoint;
        Paragraph _removedParagraph;
    }
}
