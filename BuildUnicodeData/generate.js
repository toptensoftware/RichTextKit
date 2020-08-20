const fs = require('fs');
const UnicodeTrieBuilder = require('unicode-trie/builder');

var PairedBracketType = {
  n : 0,      // Not a bracket
  o : 1,      // Opening bracket
  c : 2       // Closing bracket
};

var WordBoundaryClass = {
  AlphaDigit : 0,
  Ignore : 1,
  Space : 2,
  Punctuation : 3,
};

var GraphemeClusterClass = {
  Any : 0,
  CR : 1,
  LF : 2,
  Control : 3,
  Extend : 4,
  L : 5,
  V : 6,
  T : 7,
  LV : 8,
  LVT : 9
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
  OP: 0,   // Opening punctuation
  CL: 1,   // Closing punctuation
  CP: 2,   // Closing parenthesis
  QU: 3,   // Ambiguous quotation
  GL: 4,   // Glue
  NS: 5,   // Non-starters
  EX: 6,   // Exclamation/Interrogation
  SY: 7,   // Symbols allowing break after
  IS: 8,   // Infix separator
  PR: 9,   // Prefix
  PO: 10,  // Postfix
  NU: 11,  // Numeric
  AL: 12,  // Alphabetic
  HL: 13,  // Hebrew Letter
  ID: 14,  // Ideographic
  IN: 15,  // Inseparable characters
  HY: 16,  // Hyphen
  BA: 17,  // Break after
  BB: 18,  // Break before
  B2: 19,  // Break on either side (but not pair)
  ZW: 20,  // Zero-width space
  CM: 21,  // Combining marks
  WJ: 22,  // Word joiner
  H2: 23,  // Hangul LV
  H3: 24,  // Hangul LVT
  JL: 25,  // Hangul L Jamo
  JV: 26,  // Hangul V Jamo
  JT: 27,  // Hangul T Jamo
  RI: 28,  // Regional Indicator
  EB: 29,  // Emoji Base
  EM: 30,  // Emoji Modifier
  ZWJ: 31, // Zero Width Joiner
  CB: 32,  // Contingent break
  AI: 33,  // Ambiguous (Alphabetic or Ideograph)
  BK: 34,  // Break (mandatory)
  CJ: 35,  // Conditional Japanese Starter
  CR: 36,  // Carriage return
  LF: 37,  // Line feed
  NL: 38,  // Next line
  SA: 39,  // South-East Asian
  SG: 40,  // Surrogates
  SP: 41,  // Space
  XX: 42,  // Unknown
}

var bidi = {};

const wordBoundaryClassesTrie = new UnicodeTrieBuilder(WordBoundaryClass.AlphaDigit);
const graphemeClusterClassesTrie = new UnicodeTrieBuilder(GraphemeClusterClass.Any);
const lineBreakClassesTrie = new UnicodeTrieBuilder(LineBreakClass.XX);
const bidiClassesTrie = new UnicodeTrieBuilder(0);

function processUnicodeData()
{
  // http://www.unicode.org/Public/UNIDATA/UnicodeData.txt
  var data = fs.readFileSync("UnicodeData.txt", "utf8")
  var lines = data.split('\n');

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

          // Build cluster trie
          switch (parts[2])
          {
              case "Cc":
              case "Cf":
              case "Zl":
              case "Zp":
                if (codePoint == 0x0D)  
                  graphemeClusterClassesTrie.set(codePoint, GraphemeClusterClass.CR);
                else if (codePoint == 0x0A)
                  graphemeClusterClassesTrie.set(codePoint, GraphemeClusterClass.LF);
                else if (codePoint != 0x200C && codePoint != 0x200D)
                  graphemeClusterClassesTrie.set(codePoint, GraphemeClusterClass.Control);
                break;
          }

          // Build word boundary trie
          switch (parts[2])
          {
              case "Cc":
              case "Cf":
              case "Cs":
              case "Co":
              case "Cn":
              case "Mc":
              case "Zs":
              case "Zl":
              case "Zp":
                wordBoundaryClassesTrie.set(codePoint, WordBoundaryClass.Space);
                break;

              case "Pc":
              case "Pd":
              case "Ps":
              case "Pe":
              case "Pi":
              case "Pf":
              case "Po":
              case "Sm":
              case "Sc":
              case "Sk":
              case "So":
                wordBoundaryClassesTrie.set(codePoint, WordBoundaryClass.Punctuation);
                break;

              case "Nd":
              case "Nl":
              case "No":
              case "Lu":
              case "Ll":
              case "Lt":
              case "LC":
              case "Lm":
              case "Lo":
                wordBoundaryClassesTrie.set(codePoint, WordBoundaryClass.AlphaDigit);
                break;

              case "Mn":
              case "Me":
                wordBoundaryClassesTrie.set(codePoint, WordBoundaryClass.None);
                break;
              
              default:
                throw new Error(`Unrecognized general category: ${parts[2]}`);
          }

      }
  }
}

  
function processDerivedCoreProperties()
{
  var data = fs.readFileSync("DerivedCoreProperties.txt", "utf8")

  var re = /^([0-9A-F]+)(?:\.\.([0-9A-F]+))?\s*;\s*(.*?)\s*#/gm
  var m;
  while (m = re.exec(data))
  {
    var from = parseInt(m[1], 16);
    var to = m[2] === undefined ? from : parseInt(m[2], 16);
    var prop = m[3];

    if (prop == "Grapheme_Extend")
    {
      graphemeClusterClassesTrie.setRange(from, to, GraphemeClusterClass.Extend);
    }
  }
}

function processHangulSyllableTypes()
{
  var data = fs.readFileSync("HangulSyllableType.txt", "utf8")

  var re = /^([0-9A-F]+)(?:\.\.([0-9A-F]+))?\s*;\s*(.*?)\s*#/gm
  var m;
  while (m = re.exec(data))
  {
    var from = parseInt(m[1], 16);
    var to = m[2] === undefined ? from : parseInt(m[2], 16);
    var prop = m[3];

    switch (prop)
    {
      case "L":
        graphemeClusterClassesTrie.setRange(from, to, GraphemeClusterClass.L, true);
        break;
      case "V":
        graphemeClusterClassesTrie.setRange(from, to, GraphemeClusterClass.V, true);
        break;
      case "T":
        graphemeClusterClassesTrie.setRange(from, to, GraphemeClusterClass.T, true);
        break;
      case "LV":
        graphemeClusterClassesTrie.setRange(from, to, GraphemeClusterClass.LV, true);
        break;
      case "LVT":
        graphemeClusterClassesTrie.setRange(from, to, GraphemeClusterClass.LVT, true);
        break;
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
  processBidiBrackets();

  var keys = Object.keys(bidi);
  for (var i=0; i<keys.length; i++)
  {
    if (bidi[keys[i]] != 0)
    {
      var cp = parseInt(keys[i]);
      bidiClassesTrie.set(cp, bidi[cp]);
    }
  }

}


function buildLineBreaksTrie()
{
  // http://www.unicode.org/Public/7.0.0/ucd/LineBreak.txt'
  var data = fs.readFileSync("LineBreak.txt", "utf8");

  const matches = data.match(/^[0-9A-F]+(\.\.[0-9A-F]+)?;[A-Z][A-Z0-9][A-Z]?/gm);

  let start = null;
  let end = null;
  let type = null;

  // collect entries in the linebreaking table into ranges
  // to keep things smaller.
  for (let match of matches) {
    var rangeEnd, rangeType;
    match = match.split(/;|\.\.\.?/);
    const rangeStart = match[0];

    if (match.length === 3) {
      rangeEnd = match[1];
      rangeType = match[2];
    } else {
      rangeEnd = rangeStart;
      rangeType = match[1];
    }

    if ((type != null) && (rangeType !== type)) {
      lineBreakClassesTrie.setRange(parseInt(start, 16), parseInt(end, 16), LineBreakClass[type], true);
      type = null;
    }

    if ((type == null)) {
      start = rangeStart;
      type = rangeType;
    }

    end = rangeEnd;
  }

  lineBreakClassesTrie.setRange(parseInt(start, 16), parseInt(end, 16), LineBreakClass[type], true);
}

processUnicodeData();
processDerivedCoreProperties();
processHangulSyllableTypes();
buildBidiTrie();
buildLineBreaksTrie();

fs.writeFileSync(__dirname + '/../Topten.RichTextKit/Resources/GraphemeClusterClasses.trie', graphemeClusterClassesTrie.toBuffer());
fs.writeFileSync(__dirname + '/../Topten.RichTextKit/Resources/WordBoundaryClasses.trie', wordBoundaryClassesTrie.toBuffer());
fs.writeFileSync(__dirname + '/../Topten.RichTextKit/Resources/LineBreakClasses.trie', lineBreakClassesTrie.toBuffer());
fs.writeFileSync(__dirname + '/../Topten.RichTextKit/Resources/BidiClasses.trie', bidiClassesTrie.toBuffer());
