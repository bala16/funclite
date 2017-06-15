using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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
        public JObject Get(string name)
        {
            JObject functionWireObject = new JObject();
            functionWireObject.Add("name", name);
            return functionWireObject;
        }

        [HttpGet("{name}/versions")]
        public IActionResult GetVersions(string name)
        {
            try
            {
                var func = _funcManager.GetFunction(Language.Node, name, throwIfNotFound: false) ?? _funcManager.GetFunction(Language.Ruby, name, throwIfNotFound: true);
                return Ok(func.GetVersions());
            }
            catch (FileNotFoundException e)
            {
                return NotFound(e.Message);
            }
        }

        // POST api/functions/foo
        [HttpPost("{name}")]
        public async Task<IActionResult> Post([FromRoute] string name, IFormCollection formData)
        {
            var language = formData.FirstOrDefault(kvp => kvp.Key.Equals("language")).Value.FirstOrDefault();
            var file = formData.Files.FirstOrDefault(f => f.FileName.EndsWith(".zip"));

            if (language == null || file == null)
            {
                return BadRequest();
            }

            if (!Enum.TryParse(language, true, out Language langType))
            {
                return new UnsupportedMediaTypeResult();
            }

            await _funcManager.Create(langType, name, file.OpenReadStream());

            return Ok(new { result = "function created" });
        }

        // POST api/functions/foo/run
        [HttpPost]
        [Route("{name}/run")]
        public async Task<dynamic> Run(string name, [FromBody]JObject requestBody)
        {
            try
            {
                return await _funcManager.Run(name, null, requestBody);
            }
            catch (FileNotFoundException e)
            {
                return NotFound(e.Message);
            }
        }

        // POST api/functions/foo/versions/17/run
        [HttpPost]
        [Route("{name}/versions/{version}/run")]
        public async Task<dynamic> RunVersion(string name, int? version, [FromBody]JObject requestBody)
        {
            try
            {
                return await _funcManager.Run(name, version, requestBody);
            }
            catch (FileNotFoundException e)
            {
                return NotFound(e.Message);
            }
        }

        // DELETE api/functions/foo
        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete(string name)
        {
            try
            {
                await _funcManager.DeleteFunction(name);
            }
            catch (FileNotFoundException e)
            {
                return NotFound(e.Message);
            }

            return Ok();
        }

        [HttpDelete("{name}/versions/{version}")]
        public async Task<IActionResult> DeleteVersion(string name, int version)
        {
            try
            {
                var function = _funcManager.GetFunction(Language.Node, name, throwIfNotFound: false) ?? _funcManager.GetFunction(Language.Ruby, name, throwIfNotFound: true);
                await function.DeleteVersion(version);
            }
            catch (FileNotFoundException e)
            {
                return NotFound(e.Message);
            }

            return Ok();
        }
    }
}
