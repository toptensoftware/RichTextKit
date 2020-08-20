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
            if (semantics == EditSemantics.Typing && this.Semantics == EditSemantics.Typing)
            {
                // Mustn't be replacing a range and must be at the correct position
                if (range.IsRange || CodePointIndex + NewLength != range.Start)
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

                // Update the group
                NewLength += codePoints.Length;

                // Fire notifications
                context.FireDocumentWillChange();
                context.FireDocumentChange(new DocumentChangeInfo()
                {
                    CodePointIndex = range.Start,
                    OldLength = 0,
                    NewLength = codePoints.Length,
                    Semantics = semantics,
                });
                context.FireDocumentDidChange();
                return true;
            }

            // Extend backspace?
            if (semantics == EditSemantics.Backspace && this.Semantics == EditSemantics.Backspace)
            {
                // Get the last delete unit
                var deleteUnit = this.LastUnit as UndoDeleteText;
                if (deleteUnit == null)
                    return false;

                // Must be deleting text immediately before
                if (range.End != this.CodePointIndex)
                    return false;

                // Extend the delete unit
                if (!deleteUnit.ExtendBackspace(range.Length))
                    return false;

                // Update self
                CodePointIndex -= range.Length;
                OldLength += range.Length;

                // Fire change events
                context.FireDocumentWillChange();
                context.FireDocumentChange(new DocumentChangeInfo()
                {
                    CodePointIndex = range.Start,
                    OldLength = range.Length,
                    NewLength = 0,
                    Semantics = semantics,
                });
                context.FireDocumentDidChange();
                return true;
            }

            // Extend delete forward?
            if (semantics == EditSemantics.ForwardDelete && this.Semantics == EditSemantics.ForwardDelete)
            {
                // Get the last delete unit
                var deleteUnit = this.LastUnit as UndoDeleteText;
                if (deleteUnit == null)
                    return false;

                // Must be deleting text immediately after
                if (range.Start != this.CodePointIndex)
                    return false;

                // Extend the delete unit
                if (!deleteUnit.ExtendForwardDelete(range.Length))
                    return false;

                // Update self
                OldLength += range.Length;

                // Fire change events
                context.FireDocumentWillChange();
                context.FireDocumentChange(new DocumentChangeInfo()
                {
                    CodePointIndex = range.Start,
                    OldLength = range.Length,
                    NewLength = 0,
                    Semantics = semantics,
                });
                context.FireDocumentDidChange();
                return true;
            }

            return false;
        }

        public override void OnOpen(TextDocument context)
        {
            base.OnOpen(context);
        }

        public override void OnClose(TextDocument context)
        {
            base.OnClose(context);

            context.FireDocumentChange(new DocumentChangeInfo()
            {
                CodePointIndex = CodePointIndex,
                OldLength = OldLength,
                NewLength = NewLength,
                Semantics = Semantics,
            });
        }

        public override void Undo(TextDocument context)
        {
            base.Undo(context);

            context.FireDocumentChange(new DocumentChangeInfo()
            {
                CodePointIndex = CodePointIndex,
                OldLength = NewLength,
                NewLength = OldLength,
                IsUndoing = true,
                Semantics = Semantics,
            });
        }

        public override void Redo(TextDocument context)
        {
            base.Redo(context);

            context.FireDocumentChange(new DocumentChangeInfo()
            {
                CodePointIndex = CodePointIndex,
                OldLength = OldLength,
                NewLength = NewLength,
                Semantics = Semantics,
            });
        }

        public int CodePointIndex { get; set; }
        public int OldLength { get; set; }
        public int NewLength { get; set; }
        public EditSemantics Semantics { get; set; }
    }
}
