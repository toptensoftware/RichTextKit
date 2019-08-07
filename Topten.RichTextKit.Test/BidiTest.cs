using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Topten.RichTextKit.Utils;
using Xunit;

namespace Topten.RichTextKit.Test
{
    public class BidiTest
    {
        [Fact]
        public void Test()
        {
            Assert.True(TestBench.BidiTest.Run());
            Assert.True(TestBench.BidiCharacterTest.Run());
        }
    }
}
