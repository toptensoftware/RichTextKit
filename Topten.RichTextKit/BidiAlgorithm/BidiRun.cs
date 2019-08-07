using System;
using System.Collections.Generic;
using System.Text;
using Topten.RichTextKit.Utils;

namespace Topten.RichTextKit
{
    internal struct BidiRun
    {
        public Directionality Direction;
        public int Start;
        public int Length;
        public int End => Start + Length;

        public override string ToString()
        {
            return $"{Start} - {End} - {Direction}";
        }

        public static IEnumerable<BidiRun> CoalescLevels(Slice<sbyte> levels)
        {
            if (levels.Length == 0)
                yield break;

            int startRun = 0;
            sbyte runLevel = levels[0];
            for (int i = 1; i < levels.Length; i++)
            {
                if (levels[i] == runLevel)
                    continue;

                // End of this run
                yield return new BidiRun()
                {
                    Direction = (runLevel & 0x01) == 0 ? Directionality.L : Directionality.R,
                    Start = startRun,
                    Length = i - startRun,
                };

                // Move to next run
                startRun = i;
                runLevel = levels[i];
            }

            yield return new BidiRun()
            {
                Direction = (runLevel & 0x01) == 0 ? Directionality.L : Directionality.R,
                Start = startRun,
                Length = levels.Length - startRun,
            };
        }
    }
}
