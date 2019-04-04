using EtherChain.Models;
using Microsoft.AspNetCore.Mvc;

namespace EtherChain.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AddressController : ControllerBase
    {
        // GET
        [HttpGet("{id}")]
        public ActionResult<Address> Get(string id)
        {
            return Program.db.GetAddress(id);
        }
    }
}