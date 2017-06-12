using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

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
        public string Get(int name)
        {
            return "value";
        }

        // PUT api/functions/foo
        [HttpPut("{name}")]
        public void Put(int name, [FromBody]string value)
        {
        }

        // POST api/functions/foo/run
        [HttpPost]
        [Route("{name}/run")]
        public void Run(string name, [FromBody]string value)
        {

        }

        // DELETE api/functions/5
        [HttpDelete("{name}")]
        public void Delete(int name)
        {
        }
    }
}
