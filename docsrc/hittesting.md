---
title: Hit Testing
---

# Hit Testing

Hit testing lets you find the character cluster that a co-ordinate
is either directly over and/or closest to.  This can be used to be build a range 
selection feature and/or as part of a more comprehensive editor.

```csharp
// Hit test a mouse co-ordinate for example
var htr = tb.HitTest(x, y);
```

The co-ordinates passed to the [RichString.HitTest](./ref/Topten.RichTextKit.RichString.HitTest) and [TextBlock.HitTest](./ref/Topten.RichTextKit.TextBlock.HitTest)
methods must be relative to the top-left corner of the object. (ie: you'll probably
need to adjust the co-ordinates by subtracting the top-left position of where you're 
displaying the text block).

The returned [T:Topten.RichTextKit.HitTestResult] structure describes the line
and code point cluster the point is either directly over and/or closest to.



