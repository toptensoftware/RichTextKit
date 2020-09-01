using Topten.RichTextKit.Utils;

namespace Topten.RichTextKit.Editor.UndoUnits
{
    class UndoReplaceTextGroup : UndoGroup<TextDocument>
    {
        public UndoReplaceTextGroup() : base(null)
        {
        }

        public bool TryExtend(TextDocument context, TextRange range, StyledText text, EditSemantics semantics, int imeCaretOffset)
        {
            // Extend typing?
            if (semantics == EditSemantics.Typing && _info.Semantics == EditSemantics.Typing)
            {
                // Mustn't be replacing a range and must be at the correct position
                if (range.IsRange || _info.CodePointIndex + _info.NewLength != range.Start)
                    return false;

                // Mustn't be inserting any paragraph breaks
                if (text.CodePoints.AsSlice().IndexOf('\u2029') >= 0)
                    return false;

                // The last unit in this group must be an insert text unit
                if (!(LastUnit is UndoInsertText insertUnit))
                    return false;

                // Check if should extend (will return false on a word boundary 
                // to avoid extending for long spans of typed text)
                if (!insertUnit.ShouldAppend(text))
                    return false;

                // Update the insert unit
                insertUnit.Append(text);

                // Fire notifications
                context.FireDocumentWillChange();
                context.FireDocumentChange(new DocumentChangeInfo()
                {
                    CodePointIndex = _info.CodePointIndex + _info.NewLength,
                    OldLength = 0,
                    NewLength = text.Length,
                    Semantics = semantics,
                });
                context.FireDocumentDidChange();

                // Update the group
                _info.NewLength += text.Length;

                return true;
            }

            // Extend overtype?
            if (semantics == EditSemantics.Overtype && _info.Semantics == EditSemantics.Overtype)
            {
                // Mustn't be replacing a range and must be at the correct position
                if (_info.CodePointIndex + _info.NewLength != range.Start)
                    return false;

                // Mustn't be inserting any paragraph breaks
                if (text.CodePoints.AsSlice().IndexOf('\u2029') >= 0)
                    return false;

                // The last unit in this group must be an insert text unit
                if (!(LastUnit is UndoInsertText insertUnit))
                    return false;

                // The second last unit before must be a delete text unit.  
                // If we don't have one, create one.  This can happen when starting
                // to type in overtype mode at the very end of a paragraph
                if (Units.Count < 2 || (!(Units[Units.Count - 2] is UndoDeleteText deleteUnit)))
                {
                    deleteUnit = new UndoDeleteText(insertUnit.TextBlock, insertUnit.Offset, 0);
                    this.Insert(Units.Count - 1, deleteUnit);
                }

                // Delete forward if can 
                // (need to do this before insert and doesn't matter if can't)
                int deletedLength = 0;
                if (deleteUnit.ExtendOvertype(range.Start - _info.CodePointIndex, range.Length))
                    deletedLength = range.Length;

                // Extend insert unit
                insertUnit.Append(text);

                // Fire notifications
                context.FireDocumentWillChange();
                context.FireDocumentChange(new DocumentChangeInfo()
                {
                    CodePointIndex = _info.CodePointIndex + _info.NewLength,
                    OldLength = deletedLength,
                    NewLength = text.Length,
                    Semantics = semantics,
                });
                context.FireDocumentDidChange();

                // Update the group
                _info.OldLength += deletedLength;
                _info.NewLength += text.Length;

                return true;
            }

            // Extend backspace?
            if (semantics == EditSemantics.Backspace && _info.Semantics == EditSemantics.Backspace)
            {
                // Get the last delete unit
                var deleteUnit = this.LastUnit as UndoDeleteText;
                if (deleteUnit == null)
                    return false;

                // Must be deleting text immediately before
                if (range.End != _info.CodePointIndex)
                    return false;

                // Extend the delete unit
                if (!deleteUnit.ExtendBackspace(range.Length))
                    return false;

                // Fire change events
                context.FireDocumentWillChange();
                context.FireDocumentChange(new DocumentChangeInfo()
                {
                    CodePointIndex = _info.CodePointIndex - range.Length,
                    OldLength = range.Length,
                    NewLength = 0,
                    Semantics = semantics,
                });
                context.FireDocumentDidChange();

                // Update self
                _info.CodePointIndex -= range.Length;
                _info.OldLength += range.Length;

                return true;
            }

            // Extend delete forward?
            if (semantics == EditSemantics.ForwardDelete && _info.Semantics == EditSemantics.ForwardDelete)
            {
                // Get the last delete unit
                var deleteUnit = this.LastUnit as UndoDeleteText;
                if (deleteUnit == null)
                    return false;

                // Must be deleting text immediately after
                if (range.Start != _info.CodePointIndex)
                    return false;

                // Extend the delete unit
                if (!deleteUnit.ExtendForwardDelete(range.Length))
                    return false;

                // Update self
                _info.OldLength += range.Length;

                // Fire change events
                context.FireDocumentWillChange();
                context.FireDocumentChange(new DocumentChangeInfo()
                {
                    CodePointIndex = _info.CodePointIndex,
                    OldLength = range.Length,
                    NewLength = 0,
                    Semantics = semantics,
                });
                context.FireDocumentDidChange();
                return true;
            }

            // IME Composition
            if (semantics == EditSemantics.ImeComposition && _info.Semantics == EditSemantics.ImeComposition)
            {
                // The last unit in this group must be an insert text unit
                if (!(LastUnit is UndoInsertText insertUnit))
                    return false;

                // Replace the inserted text
                insertUnit.Replace(text);

                // Fire notifications
                context.FireDocumentWillChange();
                context.FireDocumentChange(new DocumentChangeInfo()
                {
                    CodePointIndex = _info.CodePointIndex,
                    OldLength = _info.NewLength,
                    NewLength = text.Length,
                    Semantics = semantics,
                    ImeCaretOffset = imeCaretOffset,
                });
                context.FireDocumentDidChange();

                // Update the group
                _info.NewLength = text.Length;
                _info.ImeCaretOffset = imeCaretOffset;

                return true;
            }

            return false;
        }

        public override void OnClose(TextDocument context)
        {
            base.OnClose(context);
            context.FireDocumentChange(_info);
        }


        public override void Undo(TextDocument context)
        {
            // Make the change
            base.Undo(context);

            // Fire the undo version of the info by swapping the
            // old and new length and setting the undo flag
            var undoInfo = _info;
            undoInfo.IsUndoing = true;
            SwapHelper.Swap(ref undoInfo.OldLength, ref undoInfo.NewLength);
            context.FireDocumentChange(undoInfo);
        }

        public override void Redo(TextDocument context)
        {
            // Make the change
            base.Redo(context);

            // Fire event
            context.FireDocumentChange(_info);
        }

        public void SetDocumentChangeInfo(DocumentChangeInfo info)
        {
            _info = info;
        }

        public DocumentChangeInfo Info => _info;

        DocumentChangeInfo _info;
    }
}
