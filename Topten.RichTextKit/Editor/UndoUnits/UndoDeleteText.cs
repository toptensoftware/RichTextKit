using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Topten.RichTextKit.Utils;

namespace Topten.RichTextKit.Editor.UndoUnits
{
    class UndoDeleteText : UndoUnit<TextDocument>
    {
        public UndoDeleteText(TextBlock textBlock, int offset, int length)
        {
            _textBlock = textBlock;
            _offset = offset;
            _length = length;
        }

        public override void Do(TextDocument context)
        {
            _savedText = _textBlock.Extract(_offset, _length);
            _textBlock.DeleteText(_offset, _length);
        }

        public override void Undo(TextDocument context)
        {
            _textBlock.InsertText(_offset, _savedText);
            _savedText = null;
        }

        public bool ExtendBackspace(int length)
        {
            // Don't extend across paragraph boundaries
            if (_offset - length < 0)
                return false;

            // Copy the additional text
            var temp = _textBlock.Extract(_offset - length, length);
            _savedText.InsertText(0, temp);
            _textBlock.DeleteText(_offset - length, length);

            // Update position
            _offset -= length;
            _length += length;

            return true;
        }

        public bool ExtendForwardDelete(int length)
        {
            // Don't extend across paragraph boundaries
            if (_offset + length > _textBlock.Length - 1)
                return false;

            // Copy the additional text
            var temp = _textBlock.Extract(_offset, length);
            _savedText.InsertText(_length, temp);
            _textBlock.DeleteText(_offset, length);

            // Update position
            _length += length;

            return true;
        }

        TextBlock _textBlock;
        int _offset;
        int _length;
        TextBlock _savedText;
    }
}
