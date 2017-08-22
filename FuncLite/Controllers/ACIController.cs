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


        [HttpPost("{containerGroupName}")]
        public async Task<IActionResult> CreateContainer(string containerGroupName, [FromBody] dynamic containerGroupDefinition)
        {
            await _aciManager.CreateContainer(containerGroupName, containerGroupDefinition);
            return Ok($"{containerGroupName} created successfully");
        }
    }
}
