using System;
using System.Collections.Generic;
using System.Numerics;

namespace EtherChain.Models
{
    [Serializable]
    public class Address
    {
        public Address()
        {
            Balance = 0;
            TrKeys = new List<long>();
        }

        public BigInteger Balance;
        public List<long> TrKeys;
    }
}