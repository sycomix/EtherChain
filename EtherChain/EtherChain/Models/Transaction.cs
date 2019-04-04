using System;
using System.Numerics;
using ZeroFormatter;

namespace EtherChain.Models
{
    [ZeroFormattable]
    public class Transaction
    {
        [Index(0)]
        public virtual string Hash { get; set; } // Transaction Id

        [Index(1)]
        public virtual string FromAddress { get; set; }

        [Index(2)]
        public virtual string ToAddress { get; set; }

        [Index(3)]
        public virtual BigInteger Amount { get; set; }

        [Index(4)]
        public virtual BigInteger Gas { get; set; }

        [Index(5)]
        public virtual BigInteger GasPrice { get; set; }

        [Index(6)]
        public virtual string BlockHash { get; set; }

        [Index(7)]
        public virtual BigInteger Nonce { get; set; }
    }
}