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
            return _funcManager.Functions;
        }

        // GET api/functions/foo
        [HttpGet("{name}")]
        public string Get(string name)
        {
            return name;
        }


        // POST api/functions/foo
        [HttpPost("{name}")]
        public async Task<IActionResult> Post([FromRoute] string name, IFormCollection formData)
        {
            string language = formData.Where(kvp => kvp.Key.Equals("language")).FirstOrDefault().Value.FirstOrDefault();
            IFormFile file = formData.Files.Where(f => f.FileName.EndsWith(".zip")).FirstOrDefault();

            await _funcManager.Create(name, file.OpenReadStream());

            return Ok(new { result = "function created" });
        }

        //TODO remove?
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
        public async Task<IActionResult> Delete(int name)
        {
            //TODO
            return NoContent();
        }
    }
}
