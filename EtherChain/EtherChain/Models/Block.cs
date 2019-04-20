using System.Collections.Generic;
using MessagePack;

namespace EtherChain.Models
{
    [MessagePackObject]
    public class Block
    {
        public Block()
        {
            TransactionIds = new Dictionary<string, List<long>>();
        }

        [Key(0)]
        public string Hash { get; set; }

        [Key(1)]
        public Dictionary<string, List<long>> TransactionIds { get; set; }
    }
}