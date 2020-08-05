using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Topten.RichTextKit.Utils;
using Xunit;

namespace Topten.RichTextKit.Test
{
    public class BufferTests
    {
        Slice<char> ToSlice(string str)
        {
            var array = str.ToCharArray();
            return new Slice<char>(array, 0, array.Length);
        }

        string FromSlice(Slice<char> chars)
        {
            return new string(chars.AsSpan());
        }

        [Fact]
        public void Add()
        {
            var buffer = new Buffer<char>();
            buffer.Add(ToSlice("abcde"));

            Assert.Equal("abcde", FromSlice(buffer.AsSlice()));
        }

        [Fact]
        public void Append()
        {
            var buffer = new Buffer<char>();
            buffer.Add(ToSlice("ab"));
            buffer.Add(ToSlice("cde"));

            Assert.Equal("abcde", FromSlice(buffer.AsSlice()));
        }

        [Fact]
        public void Prepend()
        {
            var buffer = new Buffer<char>();
            buffer.Add(ToSlice("cde"));
            buffer.Insert(0, ToSlice("ab"));

            Assert.Equal("abcde", FromSlice(buffer.AsSlice()));
        }

        [Fact]
        public void Insert()
        {
            var buffer = new Buffer<char>();
            buffer.Add(ToSlice("abcde"));
            buffer.Insert(2, ToSlice("XYZ"));

            Assert.Equal("abXYZcde", FromSlice(buffer.AsSlice()));
        }

        [Fact]
        public void DeleteStart()
        {
            var buffer = new Buffer<char>();
            buffer.Add(ToSlice("abcde"));
            buffer.Delete(0, 2);

            Assert.Equal("cde", FromSlice(buffer.AsSlice()));
        }

        [Fact]
        public void DeleteEnd()
        {
            var buffer = new Buffer<char>();
            buffer.Add(ToSlice("abcde"));
            buffer.Delete(3, 2);

            Assert.Equal("abc", FromSlice(buffer.AsSlice()));
        }

        [Fact]
        public void DeleteMiddle()
        {
            var buffer = new Buffer<char>();
            buffer.Add(ToSlice("abcde"));
            buffer.Delete(1,3);

            Assert.Equal("ae", FromSlice(buffer.AsSlice()));
        }
    }
}
