using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Topten.RichText
{
    /// <summary>
    /// Unicode directionality classes
    /// </summary>
    /// <remarks>
    /// Note, these need to match those used by the JavaScript script that
    /// generates the .trie resources
    /// </remarks>
    enum Directionality : byte
    {
        // Strong types
        L = 0,
        R = 1,
        AL = 2,

        // Weak Types
        EN = 3,
        ES = 4,
        ET = 5,
        AN = 6,
        CS = 7,
        NSM = 8,
        BN = 9,

        // Neutral Types
        B = 10,
        S = 11,
        WS = 12,
        ON = 13,

        // Explicit Formatting Types
        LRE = 14,
        LRO = 15,
        RLE = 16,
        RLO = 17,
        PDF = 18,
        LRI = 19,
        RLI = 20,
        FSI = 21,
        PDI = 22,

        /** Minimum bidi type value. */
        TYPE_MIN = 0,

        /** Maximum bidi type value. */
        TYPE_MAX = 22,

        /* Unknown */
        Unknown = 0xFF,
    }

}
