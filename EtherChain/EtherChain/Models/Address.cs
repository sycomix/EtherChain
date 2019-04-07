using System;
using System.Collections.Generic;
using System.Numerics;
using ZeroFormatter;

namespace EtherChain.Models
{
    [ZeroFormattable]
    public class Address
    {
        [Index(0)]
        public virtual BigInteger Balance { get; set; }

        [Index(1)]
        public virtual BigInteger Nonce { get; set; }

        [Index(2)]
        public virtual IList<long> TrKeys { get; set; }
    }
}