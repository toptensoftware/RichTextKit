---
title: Hit Testing
---

# Hit Testing

Hit testing lets you find the line and character cluster that a coordinate
is either directly over and/or closest to.  This can be used to be build a range 
selection feature and/or as part of a more comprehensive editor.

```csharp
// Hit test a mouse co-ordinate for example
var htr = tb.HitTest(x, y);
```

The co-ordinates passed to the [HitTest](./ref/Topten.RichTextKit.TextBlock.HitTest)
method must be relative to the top-left corner of the text block. (ie: you'll probably
need to adjust the co-ordinates by subtracting the top-left position of where you're 
displaying the text block).

The returned [T:Topten.RichTextKit.HitTestResult] structure describes the line
and code point cluster the point is either directly over and/or closest to.



