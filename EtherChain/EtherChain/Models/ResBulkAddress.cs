using System;
using System.Collections.Generic;

namespace EtherChain.Models
{
    public class FullAddressInfo
    {
        public Address address { set; get; }
        public List<Transaction> transactions { set; get; }
    }
    public class ResBulkAddress
    {
        public bool success { set; get; }
        public List<FullAddressInfo> Addresses { set; get; }
    }
}
