using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System.IO;
using Newtonsoft.Json.Linq;

namespace FuncLite.Controllers
{
    [Route("api/[controller]")]
    public class FunctionsController : Controller
    {
        readonly FunctionManager _funcManager;

        public FunctionsController(FunctionManager funcManager)
        {
            _funcManager = funcManager;
        }

        // GET api/functions
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/functions/foo
        [HttpGet("{name}")]
        public string Get(string name)
        {
            return "value";
        }

        // PUT api/functions/foo
        [HttpPut("{name}")]
        public async Task<IActionResult> Put(string name)
        {
            await _funcManager.Create(name, Request.Body);

            return Ok(new { result = "function created" });
        }

        // POST api/functions/foo/run
        [HttpPost]
        [Route("{name}/run")]
        public async Task<dynamic> Run(string name, [FromBody]JObject requestBody)
        {
            return await _funcManager.Run(name, requestBody);
        }

        // DELETE api/functions/foo
        [HttpDelete("{name}")]
        public void Delete(int name)
        {
        }
    }
}
