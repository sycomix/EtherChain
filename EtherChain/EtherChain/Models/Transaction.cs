using System;
using System.Numerics;

namespace EtherChain.Models
{
    [Serializable]
    public class Transaction
    {
        public string Hash; // Transaction Id
        public string FromAddress;
        public string ToAddress;
        public BigInteger Amount;
        public BigInteger Gas;
        public BigInteger GasPrice;
        public string BlockHash;
    }
}