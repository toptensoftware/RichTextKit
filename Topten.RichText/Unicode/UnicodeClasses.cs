using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Topten.RichText
{
    static class UnicodeClasses
    {
        static UnicodeClasses()
        {
            _bidiTrie = new UnicodeTrie(typeof(LineBreaker).Assembly.GetManifestResourceStream("Topten.RichText.Resources.BidiData.trie"));
            _classesTrie = new UnicodeTrie(typeof(LineBreaker).Assembly.GetManifestResourceStream("Topten.RichText.Resources.LineBreakClasses.trie"));
        }

        static UnicodeTrie _bidiTrie;
        static UnicodeTrie _classesTrie;

        /// <summary>
        /// Get the directionality of a Unicode Code Point
        /// </summary>
        /// <param name="codePoint">The code point in question</param>
        /// <returns>The code point's directionality</returns>
        public static Directionality Directionality(int codePoint)
        {
            return (Directionality)(_bidiTrie.Get(codePoint) >> 24);
        }

        /// <summary>
        /// Get the directionality of a Unicode Code Point
        /// </summary>
        /// <param name="codePoint">The code point in question</param>
        /// <returns>The code point's directionality</returns>
        public static uint BidiData(int codePoint)
        {
            return _bidiTrie.Get(codePoint);
        }

        /// <summary>
        /// Get the bracket type for a Unicode Code Point
        /// </summary>
        /// <param name="codePoint">The code point in question</param>
        /// <returns>The code point's paired bracked type</returns>
        public static PairedBracketType PairedBracketType(int codePoint)
        {
            return (PairedBracketType)((_bidiTrie.Get(codePoint) >> 16) & 0xFF);
        }

        /// <summary>
        /// Get the associated bracket type for a Unicode Code Point
        /// </summary>
        /// <param name="codePoint">The code point in question</param>
        /// <returns>The code point's opposite bracket, or 0 if not a bracket</returns>
        public static int AssociatedBracket(int codePoint)
        {
            return (int)(_bidiTrie.Get(codePoint) & 0xFFFF);
        }

        /// <summary>
        /// Get the line break class for a Unicode Code Point
        /// </summary>
        /// <param name="codePoint">The code point in question</param>
        /// <returns>The code point's line break class</returns>
        public static LineBreakClass LineBreakClass(int codePoint)
        {
            return (LineBreakClass)_classesTrie.Get(codePoint);
        }


    }
}
