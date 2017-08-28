using System;
using System.Threading.Tasks;
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
        public async Task<dynamic> GetApps()
        {
            return JsonConvert.SerializeObject(await _aciManager.GetApps(), Formatting.Indented);
        }

        [HttpGet("{appName}")]
        public dynamic GetContainerGroups(string appName)
        {
            return _aciManager.GetContainerGroups(appName);
        }

        // Each app is its own container group (1+)
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
