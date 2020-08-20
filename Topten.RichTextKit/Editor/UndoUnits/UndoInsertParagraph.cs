using Topten.RichTextKit.Utils;

namespace Topten.RichTextKit.Editor.UndoUnits
{
    class UndoInsertParagraph : UndoUnit<TextDocument>
    {
        public UndoInsertParagraph(int index, Paragraph paragraph)
        {
            _index = index;
            _paragraph = paragraph;
        }

        public override void Do(TextDocument context)
        {
            context._paragraphs.Insert(_index, _paragraph);
        }

        public override void Undo(TextDocument context)
        {
            context._paragraphs.RemoveAt(_index);
        }

        int _index;
        Paragraph _paragraph;
    }
}
