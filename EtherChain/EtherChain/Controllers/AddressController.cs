using EtherChain.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace EtherChain.Controllers
{
    [Route("api/[controller]")]
    [EnableCors("_myAllowSpecificOrigins")]
    [ApiController]
    public class AddressController : ControllerBase
    {
        // GET
        [HttpGet("{add}/{id}")]
        public ActionResult<Address> Get(string id, string add)
        {
            return Program.db.GetAddress(id, add);
        }

        [HttpPost("getBulk")]
        public ActionResult<ResBulkAddress> GetBulk([FromBody] ReqBulkAddress req)
        {
            if (req == null || req.addresses == null || req.addresses.Count == 0 || string.IsNullOrEmpty(req.coinName) )
            {
                return new ResBulkAddress
                {
                    success = false,
                    Addresses = null,
                };
            }

            ResBulkAddress res = new ResBulkAddress();

            foreach (string addStr in req.addresses)
            {
                Address a = Program.db.GetAddress(addStr, req.coinName);
                List<Transaction> txs = null;
                if ( req.isGetTx && a.TrKeys.Count > 0)
                {
                    txs = new List<Transaction>();
                    for( int i = 0; i < a.TrKeys.Count; i++)
                    {
                        txs.Add(Program.db.GetTransaction(a.TrKeys[i], req.coinName));
                    }
                }

                res.Addresses.Add(new FullAddressInfo {
                    address = a,
                    transactions = txs,
                });
            }

            res.success = true;

            return res;
        }
    }
}