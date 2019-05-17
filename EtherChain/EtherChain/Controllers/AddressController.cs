using EtherChain.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

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
            add = add.ToLower();
            if (add.Substring(0, 2) == "0x")
                add = add.Substring(2);

            return Program.db.GetAddress(id, add);
        }
    }
}