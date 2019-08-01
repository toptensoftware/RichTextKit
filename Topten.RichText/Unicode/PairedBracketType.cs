using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Topten.RichText
{
    /// <summary>
    /// Unicode paired bracket types
    /// </summary>
    /// <remarks>
    /// Note, these need to match those used by the JavaScript script that
    /// generates the .trie resources
    /// </remarks>
    enum PairedBracketType : byte
    {
        n,
        o,
        c
    }
}
