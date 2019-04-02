using System;
using System.Numerics;
using ZeroFormatter;
using ZeroFormatter.Formatters;

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
            // Formatter<T> can get child serializer
            return Formatter<TTypeResolver, string>.Default.Serialize(ref bytes, offset, value.ToString());
        }

        public override BigInteger Deserialize(ref byte[] bytes, int offset, DirtyTracker tracker, out int byteSize)
        {
            var uriString = Formatter<TTypeResolver, string>.Default.Deserialize(ref bytes, offset, tracker, out byteSize);
            return (uriString == null) ? 0 : BigInteger.Parse(uriString);
        }
    }
}