﻿using System;
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
            return _funcManager.FunctionNames;
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
                var function = _funcManager.GetFunction(name, throwIfNotFound: true);
                return Ok(function.GetVersions());
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
                var function = _funcManager.GetFunction(name, throwIfNotFound: true);
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
