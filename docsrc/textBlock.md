---
title: Creating a TextBlock
---

# Creating a TextBlock

The primary class  you work with when using RichTextKit is the [T:Topten.RichTextKit.TextBlock] class.  This section describes how to create a text block and add text to it.

## Create a Text block

Creating a text block is simple

```csharp
// You'll need this namespace
using Topten.RichTextKit;

// Create the text block
var tb = new TextBlock();
```

## Adding Text

Once you've created a text block, you can add text to it with the [AddText()](./ref/Topten.RichTextKit.TextBlock.AddText) method:

```csharp

// Create normal style
var styleNormal = new Style() 
{
     FontFamily = "Arial", 
     FontSize = 14
}

// Create bold italic style
var styleBoldItalic = new Style() 
{
     FontFamily = "Arial", 
     FontSize = 14,
     FontWeight = 700,
     FontItalic = true,
}

// Add text to the text block
tb.AddText("Hello World.  ", styleNormal);
tb.AddText("Welcome to RichTextKit", styleBoldItalic)

// Configure layout properties
tb.MaxWidth = 900;
tb.Alignment = TextAlignment.Center;
```

Now that you've created a text block, added some text to it and [set the layout
properties](layout), you can [render](rendering), [measure](measuring) and [hit test](hittesting) it.

## Custom IStyle Implementation

The above example uses the built in [T:Topten.RichTextKit.Style] class to define the styles to be 
used.  The Style class is a lightweight class and is a reasonable approach for 
most scenarios.

You can however provide you own implementation of [T:Topten.RichTextKit.IStyle].  This 
provides an easy way to plugin styling to a more comprehensive styling/DOM system should 
you need it.

 
## Re-using TextBlocks

TextBlocks are designed to be re-used.  For example suppose you have a label control
that uses a TextBlock to render it's content, the recommended approach for this is to:

1. Create and hold a reference to a single TextBlock instance and just render it each 
   time the control needs to be drawn.

2. When the label's text changes, instead of creating a new TextBlock instance, 
   call the existing instance's [Clear()](./ref/Topten.RichTextKit.TextBlock.Clear) method 
   and then add the updated text to the same instance.

By re-using the same TextBlock instance you can avoid pressure on the garbage collector
since the TextBlocks internally allocated arrays can be re-used.

Another approach you might consider if you have many of pieces of text that rarely need
to be redrawn, would be to create a single TextBlock element and use it to drawn multiple
different items.


