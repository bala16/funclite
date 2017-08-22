using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<dynamic> GetContainerGroups()
        {
            return await _aciManager.GetContainerGroups();
        }

        [HttpGet("{containerGroupName}")]
        public async Task<dynamic> GetContainerGroup(string containerGroupName)
        {
            return await _aciManager.GetContainerGroup(containerGroupName);
        }

        [HttpGet("{containerGroupName}/{containerName}/logs")]
        public async Task<dynamic> GetContainerLogs(string containerGroupName, string containerName)
        {
            return await _aciManager.GetContainerLogs(containerGroupName, containerName);
        }

        [HttpPost("{containerGroupName}")]
        public async Task<IActionResult> CreateContainerGroup(string containerGroupName, [FromBody] dynamic containerGroupDefinition)
        {
            await _aciManager.CreateContainerGroup(containerGroupName, containerGroupDefinition);
            return Ok($"{containerGroupName} created successfully");
        }

        [HttpDelete("{containerGroupName}")]
        public async Task<IActionResult> DeleteContainerGroup(string containerGroupName)
        {
            await _aciManager.DeleteContainerGroup(containerGroupName);
            return Ok($"{containerGroupName} deleted successfully");
        }
    }
}
