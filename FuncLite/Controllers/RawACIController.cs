using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace FuncLite.Controllers
{
    [Route("api/[controller]")]
    public class RawACIController : Controller
    {
        private readonly RawACIManager _rawAciManager;

        public RawACIController(RawACIManager rawAciManager)
        {
            _rawAciManager = rawAciManager;
        }

        [HttpGet]
        public async Task<dynamic> GetContainerGroups()
        {
            return await _rawAciManager.GetContainerGroups();
        }

        [HttpGet("{containerGroupName}")]
        public async Task<dynamic> GetContainerGroup(string containerGroupName)
        {
            return await _rawAciManager.GetContainerGroup(containerGroupName);
        }

        [HttpGet("{containerGroupName}/{containerName}/logs")]
        public async Task<dynamic> GetContainerLogs(string containerGroupName, string containerName)
        {
            return await _rawAciManager.GetContainerLogs(containerGroupName, containerName);
        }

        [HttpPost("{containerGroupName}")]
        public async Task<IActionResult> CreateContainerGroup(string containerGroupName, [FromBody] dynamic containerGroupDefinition)
        {
            await _rawAciManager.CreateContainerGroup(containerGroupName, containerGroupDefinition);
            return Ok($"{containerGroupName} created successfully");
        }

        [HttpDelete("{containerGroupName}")]
        public async Task<IActionResult> DeleteContainerGroup(string containerGroupName)
        {
            await _rawAciManager.DeleteContainerGroup(containerGroupName);
            return Ok($"{containerGroupName} deleted successfully");
        }
    }
}
