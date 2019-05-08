using System.Numerics;
using MessagePack;

namespace EtherChain.Models
{
    [MessagePackObject]
    public class Erc20Transaction
    {
        [Key(0)]
        public virtual string Hash { get; set; } // Transaction Id

        [Key(1)]
        public virtual string FromAddress { get; set; }

        [Key(2)]
        public virtual string ToAddress { get; set; }

        [Key(3)]
        public virtual BigInteger Amount { get; set; }

        [Key(4)]
        public virtual BigInteger LogIndex { get; set; }

        [Key(5)]
        public virtual BigInteger Block { get; set; }
    }
}