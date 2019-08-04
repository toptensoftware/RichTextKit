---
title: Creating a TextBlock
---

# Creating a TextBlock

The primary class  you work with when using RichTextKit is the [T:Topten.RichTextKit.TextBlock] class.  This section describes how to create a text block and add text to it.

## Create a Text block

To create a text block, simply create a new instance of the TextBlock class.  You'll 
probably also want to set some layout properties like the maximum width and the text alignment:

```csharp
// You'll need this namespace
using Topten.RichTextKit;

// Create the text block
var tb = new TextBlock();

// Configure layout properties
tb.MaxWidth = 900;
tb.Alignment = TextAlignment.Center;
```

## Creating Styles

Before you can add text to a text block, you'll need to create the styles
that will be applied to the text:

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
```

## Adding Text

Now that you've created a text block and some styles, you can add text to the 
text block with the [AddText()](./ref/Topten.RichTextKit.TextBlock.AddText) method:

```csharp
// Add text to the text block
tb.AddText("Hello World.  ", styleNormal);
tb.AddText("Welcome to RichTextKit", styleBoldItalic)
```

That's it!  You can now use the text block to [render](rendering), [measure](measuring) and [hittest](hittesting) its content.


## Custom IStyle Implementation

The above example uses the built in [T:Topten.RichTextKit.Style] class to define the styles to be 
used.  The Style class is a lightweight class and is a reasonable approach for 
most scenarios.

You can however provide you own implementation of [T:Topten.RichTextKit.IStyle].  This 
provides an easy way to plugin styling to a more comprehensive styling/DOM system should 
you need it.

 
## Re-using TextBlocks

Text blocks are designed to be re-used.  For example suppose you have a label control
that uses a text Bbock to render it's content the recommended approach would be to:

1. Create and initialize a `TextBlock` instance with the text to be displayed.

2. Render that text block instance each time the control needs to be drawn.

3. When the label's text changes, instead of creating a new text block instance, 
   call the existing instance's [Clear()](./ref/Topten.RichTextKit.TextBlock.Clear) method 
   and then add the updated text to the same instance.

By re-using the same text block instance you can avoid pressure on the garbage collector
since the existing text block's internally allocated arrays can be re-used.

Another approach you might consider if you have many of pieces of text that rarely need
to be redrawn, would be to create a single TextBlock element and use the same instance
to draw each piece of text.


