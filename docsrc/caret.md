---
title: Caret Information
---

# Caret Information

The caret is the flashing cursor that's typically shown in a text editor
to show the current insert position.  Although RichTextKit doesn't provide
any editing capabilities, it does include the ability to calculate where
the caret should be displayed.

To determine where the caret should be displayed, call the 
[GetCaretInfo](./ref/Topten.RichTextKit.TextBlock.GetCaretInfo)
method which will return a [T:Topten.RichTextKit.CaretInfo] structure describing
where the caret should be drawn, the shape of the caret (sloped for italic) and 
other useful details about the current caret position.


