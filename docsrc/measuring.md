---
title: Measuring Text
---

# Measuring RichString

The `RichString` class provides various measurements of its contained text.

* [P:Topten.RichTextKit.RichString.MeasuredWidth]
* [P:Topten.RichTextKit.RichString.MeasuredHeight]
* [P:Topten.RichTextKit.RichString.MeasuredLength]
* [P:Topten.RichTextKit.RichString.LineCount]


# Measuring TextBlock

The `TextBlock` class provides various measurements of its contained text.

* [P:Topten.RichTextKit.TextBlock.MeasuredWidth]
* [P:Topten.RichTextKit.TextBlock.MeasuredHeight]
* [P:Topten.RichTextKit.TextBlock.MeasuredLength]
* [P:Topten.RichTextKit.TextBlock.MeasuredPadding]
* [P:Topten.RichTextKit.TextBlock.MeasuredOverhang]

## Going Deeper

In addition to these basic measurements you can also inspect the text block's 
[Lines](./ref/Topten.RichTextKit.TextBlock.Lines) collection and each line's
[FontRuns](ref/Topten.RichTextKit.TextBlock.FontRuns) collection to get detailed
information about the final layout of each line and font run.

