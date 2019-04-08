using System;
using System.Numerics;
using ZeroFormatter;
using ZeroFormatter.Formatters;
using ZeroFormatter.Internal;

namespace EtherChain.Models
{
    public class BigIntegerFormatter<TTypeResolver> : Formatter<TTypeResolver, BigInteger>
        where TTypeResolver : ITypeResolver, new()
    {
        public override int? GetLength()
        {
            // If size is variable, return null.
            return null;
        }

        public override int Serialize(ref byte[] bytes, int offset, BigInteger value)
        {
            var startOffset = offset;
            var valueBytes = value.ToByteArray();
            offset += BinaryUtil.WriteByte(ref bytes, offset, (byte)valueBytes.Length);
            offset += BinaryUtil.WriteBytes(ref bytes, offset, valueBytes);
            return offset - startOffset;
        }

        public override BigInteger Deserialize(ref byte[] bytes, int offset, DirtyTracker tracker, out int byteSize)
        {
            var bytesCount = BinaryUtil.ReadByte(ref bytes, offset);
            var valueBytes = BinaryUtil.ReadBytes(ref bytes, offset + 1, bytesCount);
            byteSize = 1 + bytesCount;
            return new BigInteger(valueBytes);
        }
    }
}