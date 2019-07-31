// Ported from: https://github.com/foliojs/linebreak

namespace Topten.RichText
{
    enum LineBreakClass
    {
        // The following break classes are handled by the pair table
        OP = 0,   // Opening punctuation
        CL = 1,   // Closing punctuation
        CP = 2,   // Closing parenthesis
        QU = 3,   // Ambiguous quotation
        GL = 4,   // Glue
        NS = 5,   // Non-starters
        EX = 6,   // Exclamation/Interrogation
        SY = 7,   // Symbols allowing break after
        IS = 8,   // Infix separator
        PR = 9,   // Prefix
        PO = 10,  // Postfix
        NU = 11,  // Numeric
        AL = 12,  // Alphabetic
        HL = 13,  // Hebrew Letter
        ID = 14,  // Ideographic
        IN = 15,  // Inseparable characters
        HY = 16,  // Hyphen
        BA = 17,  // Break after
        BB = 18,  // Break before
        B2 = 19,  // Break on either side (but not pair)
        ZW = 20,  // Zero-width space
        CM = 21,  // Combining marks
        WJ = 22,  // Word joiner
        H2 = 23,  // Hangul LV
        H3 = 24,  // Hangul LVT
        JL = 25,  // Hangul L Jamo
        JV = 26,  // Hangul V Jamo
        JT = 27,  // Hangul T Jamo
        RI = 28,  // Regional Indicator

        // The following break classes are not handled by the pair table
        AI = 29,  // Ambiguous (Alphabetic or Ideograph)
        BK = 30,  // Break (mandatory)
        CB = 31,  // Contingent break
        CJ = 32,  // Conditional Japanese Starter
        CR = 33,  // Carriage return
        LF = 34,  // Line feed
        NL = 35,  // Next line
        SA = 36,  // South-East Asian
        SG = 37,  // Surrogates
        SP = 38,  // Space
        XX = 39,  // Unknown
    }
}
