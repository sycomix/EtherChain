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
        public virtual BigInteger Balance { get; set; }

        [Key(1)]
        public virtual BigInteger Nonce { get; set; }

        [Key(2)]
        public virtual IList<long> TrKeys { get; set; }
    }
}