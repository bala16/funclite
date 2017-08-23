using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FuncLite.Controllers
{
    [Route("api/[controller]")]
    public class DemoController : Controller
    {
        private readonly ServiceFabricAppManager _serviceFabricAppManager;

        public DemoController(ServiceFabricAppManager serviceFabricAppManager)
        {
            _serviceFabricAppManager = serviceFabricAppManager;
        }

        [HttpGet]
        public dynamic GetServiceFabricApps()
        {
            return JsonConvert.SerializeObject(_serviceFabricAppManager.GetApps(), Formatting.Indented);
        }

        [HttpGet("{appName}")]
        public dynamic GetApp(string appName)
        {
            var serviceFabricApp = _serviceFabricAppManager.GetApp(appName);
            if (serviceFabricApp == null)
            {
                return NotFound();
            }
            return JsonConvert.SerializeObject(serviceFabricApp);
        }

        [HttpPost("{appName}")]
        public async Task<IActionResult> CreateApp(string appName, IFormCollection formData)
        {
            var imagePath = formData.FirstOrDefault(kvp => kvp.Key.Equals("ImagePath")).Value.FirstOrDefault();
            var replicaCountString = formData.FirstOrDefault(kvp => kvp.Key.Equals("ReplicaCount")).Value.FirstOrDefault();

            if (imagePath == null)
            {
                return BadRequest();
            }

            var replicaCount = 1;
            if (replicaCountString != null)
            {
                replicaCount = Int32.Parse(replicaCountString);
            }

            if (_serviceFabricAppManager.GetApp(appName) != null)
            {
                return new StatusCodeResult(409);
            }

            await _serviceFabricAppManager.CreateApp(appName, imagePath, replicaCount);
            return Ok($"{appName} created successfully");
        }

        [HttpDelete("{appName}")]
        public async Task<IActionResult> DeleteApp(string appName)
        {
            if (_serviceFabricAppManager.GetApp(appName) == null)
            {
                return NotFound();
            }
            await _serviceFabricAppManager.DeleteApp(appName);
            //return 202?
            return Ok($"{appName} deleted successfully");
        }

        //POST/GET api/demo/<appname>/<functionName>
        [HttpGet]
        [HttpPost]
        [Route("{appName}/{functionName}")]
        public async Task<dynamic> RunFunction(string appName, string functionName, string name)
        {
            if (_serviceFabricAppManager.GetApp(appName) == null)
            {
                return NotFound();
            }
            try
            {
                return await _serviceFabricAppManager.ExecuteFunction(appName, functionName, name);
            }
            catch (Exception e)
            {
                return StatusCode(400, e.Message);
            }
        }
    }
}
