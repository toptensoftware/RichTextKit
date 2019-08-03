const fs = require('fs');
const request = require('request');
const UnicodeTrieBuilder = require('unicode-trie/builder');

var PairedBracketType = {
  // Not a bracket
  n : 0,

  // Opening bracket
  o : 1,

  // Closing bracket
  c : 2
};

var Directionality = {
  // Strong types
  L: 0,
  R: 1,
  AL: 2,

  // Weak Types
  EN: 3,
  ES: 4,
  ET: 5,
  AN: 6,
  CS: 7,
  NSM: 8,
  BN: 9,

  // Neutral Types
  B: 10,
  S: 11,
  WS: 12,
  ON: 13,

  //Explicit Formatting Types
  LRE: 14,
  LRO: 15,
  RLE: 16,
  RLO: 17,
  PDF: 18,
  LRI: 19,
  RLI: 20,
  FSI: 21,
  PDI: 22,
}

var LineBreakClass = {
  OP : 0,   // Opening punctuation
  CL : 1,   // Closing punctuation
  CP : 2,   // Closing parenthesis
  QU : 3,   // Ambiguous quotation
  GL : 4,   // Glue
  NS : 5,   // Non-starters
  EX : 6,   // Exclamation/Interrogation
  SY : 7,   // Symbols allowing break after
  IS : 8,   // Infix separator
  PR : 9,   // Prefix
  PO : 10,  // Postfix
  NU : 11,  // Numeric
  AL : 12,  // Alphabetic
  HL : 13,  // Hebrew Letter
  ID : 14,  // Ideographic
  IN : 15,  // Inseparable characters
  HY : 16,  // Hyphen
  BA : 17,  // Break after
  BB : 18,  // Break before
  B2 : 19,  // Break on either side (but not pair)
  ZW : 20,  // Zero-width space
  CM : 21,  // Combining marks
  WJ : 22,  // Word joiner
  H2 : 23,  // Hangul LV
  H3 : 24,  // Hangul LVT
  JL : 25,  // Hangul L Jamo
  JV : 26,  // Hangul V Jamo
  JT : 27,  // Hangul T Jamo
  RI : 28,  // Regional Indicator

  // The following break classes are not handled by the pair table
  AI : 29,  // Ambiguous (Alphabetic or Ideograph)
  BK : 30,  // Break (mandatory)
  CB : 31,  // Contingent break
  CJ : 32,  // Conditional Japanese Starter
  CR : 33,  // Carriage return
  LF : 34,  // Line feed
  NL : 35,  // Next line
  SA : 36,  // South-East Asian
  SG : 37,  // Surrogates
  SP : 38,  // Space
  XX : 39,  // Unknown
}

var bidi = {};

function processUnicodeData()
{
  // http://www.unicode.org/Public/UNIDATA/UnicodeData.txt
  var data = fs.readFileSync("UnicodeData.txt", "utf8")
  var lines = data.split('\n');
  const trie = new UnicodeTrieBuilder(Directionality.L);

  for (var i=0; i<lines.length; i++)
  {
      var parts = lines[i].split(';');
      if (parts.length > 1)
      {
          // Get the code point
          var codePoint = parseInt(parts[0], 16);

          // Get the directionality
          var dir = parts[4];
          var cls = Directionality[dir];

          if (cls === undefined)
          {
              console.log("Error: ", codePoint, "unknown class", dir);
          }
          else
          {
            bidi[codePoint] = cls << 24;
          }
      }
  }
}

  
function processBidiBrackets()
{
  // https://www.unicode.org/Public/UCD/latest/ucd/BidiBrackets.txt
  var data = fs.readFileSync("BidiBrackets.txt", "utf8");

  var lines = data.split('\n');
  
  for (var i=0; i<lines.length; i++)
  {
      var parts = lines[i].split('#');
      if (parts[0].trim().length == 0)
          continue;

      parts = parts[0].trim().split(';');
      if (parts.length == 3)
      {
        var codePoint = parseInt(parts[0], 16);
        var codePointOther = parseInt(parts[1], 16);
        var kind = PairedBracketType[parts[2].trim()];

        if (bidi[codePoint] === undefined)
          bidi[codePoint] = 0;

        if ((codePointOther & 0xFFFF0000) != 0)
          throw new Error("Other bracket code point out of range" + codePointOther);

        bidi[codePoint] |= (codePointOther | (kind << 16));
      }
  }
}

function buildBidiTrie()
{
  processUnicodeData();
  processBidiBrackets();

  const trie = new UnicodeTrieBuilder(0);
  var keys = Object.keys(bidi);
  for (var i=0; i<keys.length; i++)
  {
    if (bidi[keys[i]] != 0)
    {
      var cp = parseInt(keys[i]);
      //console.log(cp.toString(16), " => ", bidi[cp].toString(16));
      trie.set(cp, bidi[cp]);
    }
  }

  fs.writeFileSync(__dirname + '/../GuiKit.RichTextKit/Resources/BidiData.trie', trie.toBuffer());
}


function buildLineBreaksTrie()
{
  // http://www.unicode.org/Public/7.0.0/ucd/LineBreak.txt'
  var data = fs.readFileSync("LineBreak.txt", "utf8");

  const matches = data.match(/^[0-9A-F]+(\.\.[0-9A-F]+)?;[A-Z][A-Z0-9]/gm);

  let start = null;
  let end = null;
  let type = null;
  const trie = new UnicodeTrieBuilder(LineBreakClass.XX);

  // collect entries in the linebreaking table into ranges
  // to keep things smaller.
  for (let match of matches) {
    var rangeEnd, rangeType;
    match = match.split(/;|\.\./);
    const rangeStart = match[0];

    if (match.length === 3) {
      rangeEnd = match[1];
      rangeType = match[2];
    } else {
      rangeEnd = rangeStart;
      rangeType = match[1];
    }

    if ((type != null) && (rangeType !== type)) {
      trie.setRange(parseInt(start, 16), parseInt(end, 16), LineBreakClass[type], true);
      type = null;
    }

    if ((type == null)) {
      start = rangeStart;
      type = rangeType;
    }

    end = rangeEnd;
  }

  trie.setRange(parseInt(start, 16), parseInt(end, 16), LineBreakClass[type], true);

  // write the trie to a file
  fs.writeFileSync(__dirname + '/../GuiKit.RichTextKit/Resources/LineBreakClasses.trie', trie.toBuffer());
}

buildBidiTrie();
buildLineBreaksTrie();