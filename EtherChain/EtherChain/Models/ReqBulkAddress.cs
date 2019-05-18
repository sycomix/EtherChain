using System;
using System.Collections.Generic;

namespace EtherChain.Models
{
    public class ReqBulkAddress
    {
        public string coinName { set; get; }
        public bool isGetTx { set; get; }
        public List<string> addresses { set; get; } 
    }
}
