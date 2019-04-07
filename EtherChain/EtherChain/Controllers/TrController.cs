﻿using EtherChain.Models;
using Microsoft.AspNetCore.Mvc;

namespace EtherChain.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrController: ControllerBase
    {
        // GET
        [HttpGet("{coin}/{id}")]
        public ActionResult<Transaction> Get(long id, string coin)
        {
            return Program.db.GetTransaction(id, coin);
        }

    }
}