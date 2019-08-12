---
title: Basic Concepts
---

# Basic Concepts

The page describes some basic concepts that are important to understand
when working with RichTextKit.


## Text Blocks

RichTextKit primarily works with individual blocks of text where each is
essentially a paragraph.

RichTextKit doesn't provide document level constructs like list items, 
block quotes, tables, table cells, divs etc...  This project is strictly
about laying out a single block of text with rich (aka attributed) text
and then providing various functions for working with that text block.

RichTextKit doesn't have a concept of a DOM - that's a higher level concept 
that RichTextKit is not attempting to solve.

Text blocks can contain forced line breaks with a newline character (`\n`).
These should be considered as in-paragraph "soft returns", as opposed to
hard paragraph terminators.

Currently RichTextKit doesn't support non-text inline elements, although this
may be added in the future.

Text blocks are represented by the [T:Topten.RichTextKit.TextBlock] class.

## Styles

Styles are used to describe the display attributes of text in a text block. A 
text block is comprised of one of more runs of text each tagged with a
single style and these text runs are referred to a "style runs".

Style runs are represented by the [T:Topten.RichTextKit.StyleRun] class.

## Font Fallback

Font fallback is the process of switching to a different font when the specified
font doesn't contain the required glyphs. 

Font fallback is often required to display emoji characters (since most fonts don't
include emoji glyphs), for most complex scripts (eg: Arabic) and Asian scripts.

RichTextKit uses SkiaSharp's `MatchCharacter` function to resolve the typeface to 
use for font fallback.


## Text Shaping

For most Latin based languages, rendering text is simply a matter of placing one glyph 
after another in a left-to-right manner.  Other languages however require a more 
complicated process that often involves drawing multiple glyphs for a single character.
This process is called "text shaping".

By default Skia (and therefore ShiaSharp) doesn't do text shaping and often displays 
text incorrectly.  RichTextKit uses [HarfBuzz](https://www.freedesktop.org/wiki/Software/HarfBuzz/) 
for text shaping.


## Font Runs

Font run are derived by splitting style runs into smaller segments when text
is wrapped onto a new line, or when a font fallback is required.

Each font run represents a single run of text that uses the same font and other
style attributes for every character in the run.  

Font runs are represented by the [T:Topten.RichTextKit.FontRun] class.


## Style Runs vs Font Runs

Style runs and font runs are similar concepts but serve two different purposes:

* Style runs describe the logical view of a text block (before layout)
* Font runs describe the physical view of a text block (after layout)


## Lines

After a text block has been laid out it results in a set of lines each 
consisting of one or more font runs.

Lines are represented by the [T:Topten.RichTextKit.TextLine] class.

## Bi-Directional, LTR and RTL Text

Bi-directional text is the process of correctly displaying text for mixed 
left-to-right and right-to-left based languages.

RichTextKit includes an implementation of the Unicode Bi-directional Text 
Algorithm ([UAX #9](http://www.unicode.org/reports/tr9/)) and each text block 
has a "base direction" that controls the default layout order of text in that 
paragraph.

The text direction of spans within the text block can be controlled using
either:

* Embedded control characters (as specifed by UAX #9)
* By setting the text direction of style runs (See the [T:Topten.RichTextKit.IStyle])

When setting text direction property on style runs, the text is processed
in the same manner as an "isolating sequence" as described in UAX #9.

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

All measurements and sizes used by RichTextKit are logical.  

The final size of rendered text will depend on the configuration of the target 
Canvas scaling and it's up to you to determine the appropriate system of measurement 
to use.