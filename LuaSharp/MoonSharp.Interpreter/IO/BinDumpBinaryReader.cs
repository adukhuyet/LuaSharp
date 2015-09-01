using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MoonSharp.Interpreter.IO
{
    /// <summary>
    ///     "Optimized" BinaryReader which shares strings and use a dumb compression for integers
    /// </summary>
    internal class BinDumpBinaryReader : BinaryReader
    {
        private readonly List<string> m_Strings = new List<string>();

        public BinDumpBinaryReader(Stream s) : base(s)
        {
        }

        public BinDumpBinaryReader(Stream s, Encoding e) : base(s, e)
        {
        }

        public override int ReadInt32()
        {
            var b = ReadSByte();

            if (b == 0x7F)
                return ReadInt16();
            if (b == 0x7E)
                return base.ReadInt32();
            return b;
        }

        public override uint ReadUInt32()
        {
            var b = ReadByte();

            if (b == 0x7F)
                return ReadUInt16();
            if (b == 0x7E)
                return base.ReadUInt32();
            return b;
        }

        public override string ReadString()
        {
            var pos = ReadInt32();

            if (pos < m_Strings.Count)
            {
                return m_Strings[pos];
            }
            if (pos == m_Strings.Count)
            {
                var str = base.ReadString();
                m_Strings.Add(str);
                return str;
            }
            throw new IOException("string map failure");
        }
    }
}