using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace FuncLite.Controllers
{
    [Route("api/[controller]")]
    public class ACIController : Controller
    {
        private readonly ACIManager _aciManager;

        public ACIController(ACIManager aciManager)
        {
            _aciManager = aciManager;
        }

        [HttpGet]
        public dynamic GetApps()
        {
            return JsonConvert.SerializeObject(_aciManager.GetApps(), Formatting.Indented);
        }

        // Each app is its own container group
        [HttpPost("{appName}")]
        public async Task<IActionResult> CreateApp(string appName, [FromBody] dynamic appDefinition)
        {
            await _aciManager.CreateApp(appName, appDefinition);
            return Ok($"{appName} created successfully");
        }

        [HttpGet("{appName}/ipaddress")]
        public async Task<string> GetIpAddress(string appName)
        {
            return await _aciManager.GetIpAddressFromARM(appName);
        }

        [HttpPost("{containerGroupName}/{functionName}")]
        [HttpGet("{containerGroupName}/{functionName}")]
        public async Task<dynamic> RunFunction(string containerGroupName, string functionName, string name)
        {
            try
            {
                return await _aciManager.RunFunction(containerGroupName, functionName, name);
            }
            catch (Exception e)
            {
                return StatusCode(400, e.Message);
            }
        }

        [HttpDelete("{appName}")]
        public async Task<IActionResult> DeleteApp(string appName)
        {
            await _aciManager.DeleteApp(appName);
            return Ok($"{appName} deleted successfully");
        }
    }
}
