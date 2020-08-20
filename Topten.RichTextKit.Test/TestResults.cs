using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Topten.RichTextKit.Test
{
    public class TestResults
    {
        public TestResults()
        {
            // Capture GC Counts
            System.GC.Collect();
            for (int i = 0; i < System.GC.MaxGeneration; i++)
            {
                _preGCCounts.Add(System.GC.CollectionCount(i));
            }

            // Reset stop watch
            sw.Reset();
            _memUsed = 0;
            _testCount = 0;
            _passCount = 0;
        }

        List<long> _preGCCounts = new List<long>();
        Stopwatch sw = new Stopwatch();
        long _memUsed = 0L;
        int _testCount = 0;
        int _passCount = 0;
        long _memBefore;

        public void EnterTest()
        {
            // update test count
            _testCount++;

            /*
            if ((testCount % 10000) == 0)
            {
                 Console.WriteLine($"Progress: line {testCount} of {tests.Count}");
            }
            */

            // record memory usage before the test
            _memBefore = System.GC.GetTotalMemory(false);

            sw.Start();
        }

        public void LeaveTest()
        {
            sw.Stop();

            // Update memory used in the test
            var memAfter = System.GC.GetTotalMemory(false);
            if (memAfter > _memBefore)
                _memUsed += (memAfter - _memBefore);
        }

        public void TestPassed(bool passed)
        {
            if (passed)
                _passCount++;
        }

        public void Dump()
        {
            Console.WriteLine();

            Console.WriteLine($"Passed {_passCount} of {_testCount} tests");
            Console.WriteLine($"Time in algorithm: {sw.Elapsed}");
            Console.WriteLine($"Memory in algorithm: {_memUsed:n0}");
            // Display collection counts.
            Console.WriteLine($"GC Collection Counts:");
            for (int i = 0; i < GC.MaxGeneration; i++)
            {
                Console.WriteLine($"  - generation #{i}: {GC.CollectionCount(i) - _preGCCounts[i]}");
            }
            Console.WriteLine();
        }

        public bool AllPassed => _testCount == _passCount;
    }
}
