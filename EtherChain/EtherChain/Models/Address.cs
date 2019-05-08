using System;
using System.Collections.Generic;
using System.Numerics;
using MessagePack;

namespace EtherChain.Models
{
    [MessagePackObject]
    public class Address
    {
        [Key(0)]
        public BigInteger Balance { get; set; }

        [Key(1)]
        public BigInteger Nonce { get; set; }

        [Key(2)]
        public List<long> TrKeys { get; set; }
    }
}