# Changelog

All notable changes to RichTextKit. For versions before 0.6.170, see the
`b<version>` tags in the git history.

## Next release

### Fixed

- `TextBlock.MeasuredOverhang` measured the wrong glyphs for any run after a
  composed character or ligature (#106)
- `StyleManager` could return a cached style with the wrong font width when
  the width was inherited from the current style (#108)
- `TextLine.HitTest` could throw for an empty trailing run (#112)
- `TextBlock.HitTestLine` threw if called before the block was laid out
- Text buffers grew by the exact amount needed instead of doubling, causing
  repeated reallocations (#104)
- `TextBlock.ResetPooledMemory` stopped the pools resetting returned objects

### Performance

- Font runs and lines are returned to their pools on relayout, not just on
  `Clear()` (#120)
- Font fallback disposes its `SKFont`s and allocates less per layout (#122)
- Text shaping reuses one HarfBuzz buffer per thread and no longer copies glyph
  data (#124)
- `TextBlock.Paint` reuses its paint context and paints, and no longer
  allocates per paint (#126)

### Other

- Removed the unneeded `System.ValueTuple` package reference (#100)
- The NuGet package now includes the readme and release notes

## 0.6.170

- Upgraded to SkiaSharp 4.153.1 (#118)
