---
title: Rendering Text
---

# Rendering Text

Text can be rendered using  [RichString.Paint()]((./ref/Topten.RichTextKit.RichString.Paint)) or [TextBlock.Paint()]((./ref/Topten.RichTextKit.TextBlock.Paint)) methods:

~~~csharp
// Paint at (0,0)
textBlockOrRichString.Paint(canvas);
~~~

By default the text will be rendered at the position (0,0).  `Use SKCanvas.Translate()` to set
the paint position or pass the position as a second parameter:

~~~csharp
// Paint at (100,100)
textBlockOrRichString.Paint(canvas, new SKPoint(100,100));
~~~


## Rendering with a Selection Highlight

Text can also be rendered with part of the text highlighted.

~~~csharp
// Highlight code points 10 through 19...
var options = new TextPaintOptions()
{
    SelectionStart = 10,
    SelectionEnd = 20,
    SelectionColor = new SKColor(0xFFFF0000),
}

// Paint with options
textBlockOrRichString.Paint(canvas, new SKPaint(100,100), options);
~~~


## Other Render Options

[T:Topten.RichTextKit.TextPaintOptions] also has settings to control anti-aliasing and LCD text rendering.

