using Topten.RichTextKit.Utils;

namespace Topten.RichTextKit.Editor.UndoUnits
{
    class UndoReplaceTextGroup : UndoGroup<TextDocument>
    {
        public UndoReplaceTextGroup() : base(null)
        {
        }

        public bool TryExtend(TextDocument context, TextRange range, Slice<int> codePoints, EditSemantics semantics)
        {
            // Extend typing?
            if (semantics == EditSemantics.Typing && _info.Semantics == EditSemantics.Typing)
            {
                // Mustn't be replacing a range and must be at the correct position
                if (range.IsRange || _info.CodePointIndex + _info.NewLength != range.Start)
                    return false;

                // Mustn't be inserting any paragraph breaks
                if (codePoints.IndexOf('\u2029') >= 0)
                    return false;

                // The last unit in this group must be an insert text unit
                if (!(LastUnit is UndoInsertText insertUnit))
                    return false;

                // Try to extend (will return false on a word boundary to avoid
                // extending for long spans of typed text)
                if (!insertUnit.Append(codePoints))
                    return false;

                // Fire notifications
                context.FireDocumentWillChange();
                context.FireDocumentChange(new DocumentChangeInfo()
                {
                    CodePointIndex = _info.CodePointIndex + _info.NewLength,
                    OldLength = 0,
                    NewLength = codePoints.Length,
                    Semantics = semantics,
                });
                context.FireDocumentDidChange();

                // Update the group
                _info.NewLength += codePoints.Length;

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

        DocumentChangeInfo _info;
    }
}
