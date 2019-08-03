using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Topten.RichTextKit
{
    static class BinaryReaderExtensions
    {
        public static int ReadInt32BE(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        public static uint ReadUInt32BE(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        public static void WriteBE(this BinaryWriter writer, int value)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            writer.Write(bytes);
        }

        public static void WriteBE(this BinaryWriter writer, uint value)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            writer.Write(bytes);
        }

    }
}
