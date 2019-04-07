﻿using EtherChain.Models;
using Microsoft.AspNetCore.Mvc;

namespace EtherChain.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AddressController : ControllerBase
    {
        // GET
        [HttpGet("{add}/{id}")]
        public ActionResult<Address> Get(string id, string add)
        {
            return Program.db.GetAddress(id, add);
        }
    }
}