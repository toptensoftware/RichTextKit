using System;
using System.Collections.Generic;
using System.Text;

namespace Topten.RichTextKit
{
    public static class SwapHelper
    {
        public static void Swap<T>(ref T a, ref T b)
        {
            var temp = a;
            a = b;
            b = temp;
        }
    }
}
