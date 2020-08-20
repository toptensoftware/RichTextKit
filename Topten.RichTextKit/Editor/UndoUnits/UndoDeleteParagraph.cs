using Topten.RichTextKit.Utils;

namespace Topten.RichTextKit.Editor.UndoUnits
{
    class UndoDeleteParagraph : UndoUnit<TextDocument>
    {
        public UndoDeleteParagraph(int index)
        {
            _index = index;
        }

        public override void Do(TextDocument context)
        {
            _paragraph = context._paragraphs[_index];
            context._paragraphs.RemoveAt(_index);
        }

        public override void Undo(TextDocument context)
        {
            context._paragraphs.Insert(_index, _paragraph);
        }

        int _index;
        Paragraph _paragraph;
    }
}
