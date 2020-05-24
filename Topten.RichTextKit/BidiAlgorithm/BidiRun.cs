// RichTextKit
// Copyright © 2019-2020 Topten Software. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may 
// not use this product except in compliance with the License. You may obtain 
// a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
// License for the specific language governing permissions and limitations 
// under the License.

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
