using EtherChain.Models;
using Microsoft.AspNetCore.Mvc;

namespace EtherChain.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrController: ControllerBase
    {
        // GET
        [HttpGet("{id}")]
        public ActionResult<Transaction> Get(long id)
        {
            return Program.db.GetTransaction(id);
        }

    }
}