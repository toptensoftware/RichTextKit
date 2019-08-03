---
title: Basic Concepts
---

# Basic Concepts

The page describes some basic concepts that are important to understand
when working with RichTextKit.


## Text Blocks

RichTextKit primarily works with text blocks.  A text block represents
a single block of text - essentially a paragraph.  

RichTextKit doesn't provide higher level constructs like list items, 
block quotes, tables, table cells, divs etc...  This project is strictly
about laying out a single block of text with rich (aka attributed) text
and then providing various functions for working with that text block.

RichTextKit doesn't have a concept of a DOM - that's a higher level concept 
that RichTextKit is not attempting to solve.

Although RichTextKit works with individual text blocks, they can contain
carriage returns (`\n`) to force line breaks.  These should be considered as 
in-paragraph "soft returns", as opposed to a hard paragraph terminator.

Currently RichTextKit doesn't support non-text inline elements, although this
may be added in the future.

Text blocks are represented by the [T:Topten.RichTextKit.TextBlock] class.

## Styles

Styles are used to describe the display attributes of text in a text block. A 
text block is comprised of one of more runs of text each tagged with a
single style.

RichTextKit defines an interface [T:Topten.RichTextKit.IStyle] that provides the style of a
run of text.  You can either provide your own implementation of this 
interface (eg: suppose you have your own DOM that computes styles), or 
you can use the built in [T:Topten.RichTextKit.Style] class which provides a simple implementation
(but doesn't have any concept if style inheritance or cascading).

Style runs are represented by the [T:Topten.RichTextKit.StyleRun] class.

## Font Fallback

Font fallback is the process of switching to a different font when the specified
font doesn't contain the required glyphs. 

Font fallback is often required to display emoji characters (since most fonts don't
include emoji glyphs), for most complex scripts (eg: Arabic) and Asian scripts.

RichTextKit uses SkiaSharp's `MatchCharacter` function to resolve the typeface to 
use for font fallback.


## Font Shaping

For most Latin based languages, rendering text is simply a matter of placing one glyph 
after another in a left-to-right manner.  

For many other languages however the process is much more complicated and often involves
drawing multiple glyphs for a single character. This process is called "font shaping"

By default Skia (and thereforce ShiaSharp) doesn't do font shaping and often displays 
text incorrectly.  RichTextKit uses HarfBuzzSharp for text shaping.


## Font Runs

A font run represents a single run of text that uses the same font and other
style attributes for every character in the run.  Font runs are derived by 
splitting style runs into smaller segments when when text is wrapped onto a 
new line, or when a font fallback is required.

Font runs are represented by the [T:Topten.RichTextKit.FontRun] class.


## Style Runs vs Font Runs

Style runs should be considered the client concept of a run of text.  ie: as it
was added to the text block.  

Font runs describe how a text block is broken down after the block has been laid 
out and can be considered the internal view of the text block.


## Lines

After a text block has been laid out it results in a set of lines each 
consisting of one or more font runs.

Lines are represented by the [T:Topten.RichTextKit.TextLine] class.

## Bi-Directional, LTR and RTL Text

Bi-directional text is the process of correctly displaying text for mixed 
left-to-right and right-to-left based languages.

RichTextKit includes an implementation of the Unicode Bi-directional Text 
Algorithm (UAX #9) and each text block has a "base direction" that controls
the default layout order of text in that paragraph.

Embedded control characters (as specifed by UAX #9) can be used to control 
bi-directional text formatting.  

Specifying the text direction of a text run through styles is currently not
supported and embedded control characters must be used.


## UTF-16, UTF-32, Code Points and Clusters

Internally, RichTextKit works with UTF-32 encoded text.  Specifically it uses the
`int` type to represent a character rather than the `char` type.

To avoid confusion, the API and code base use the term "code point" to 
refer to a single UTF-32 character.

Since C# strings are encoded as UTF-16, when a string is added to a text
block it will be automatically converted to UTF-32.  It's important to note that
indexes into the converted string may no longer match indicies into the original 
C# string. (although RichTextKit does provide functions to map between them).

RichTextKit uses the term `CodePointIndex` to refer to the index of a code point 
in a UTF-32 buffer.

Also note that line endings `\r` and `\r\n` are automatically normalized to `\n`.  
This conversion can also affect the mapping of code point indicies to character 
indicies in the original string.

*(Note: `\n\r` style line endings aren't supported)*

Some complex scripts require more that one code point to describe a single user 
perceived character.  These groupings of code points are called "clusters".  This is 
the same concept as used by HarfBuzz.


## Measurement and Size Units

All measurements and sizes used by RichTextKit are logical.  The final size of the
rendered text will depend on the configuration of the target Canvas scaling and 
it's up to you to determine the appropriate system of measurement to use.