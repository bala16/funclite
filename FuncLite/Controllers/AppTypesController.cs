using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace FuncLite.Controllers
{
    [Route("api/[controller]")]
    public class AppTypesController : Controller
    {
        private readonly ClusterManager _clusterManager;

        public AppTypesController(ClusterManager clusterManager)
        {
            _clusterManager = clusterManager;
        }

        // GET api/apptypes
        [HttpGet]
        public async Task<dynamic> GetApplicationTypes()
        {
            return await _clusterManager.GetApplicationTypes();
        }

        // GET api/apptypes/<appTypeName>
        [HttpGet("{appTypeName}")]
        public async Task<dynamic> GetApplicationTypeInfo(string appTypeName)
        {
            return await _clusterManager.GetApplicationTypeInfo(appTypeName);
        }

        // POST api/apptypes/<imagePath>
        [HttpPost("{imagePath}")]
        public async Task<dynamic> ProvisionAppType(string imagePath)
        {
            await _clusterManager.ProvisionAppType(imagePath);
            return Ok(new {result = $"{imagePath} provisioned successfully"});
        }

        // DELETE api/apptypes/<appTypeName>
        [HttpDelete("{appTypeName}")]
        public async Task<dynamic> UnprovisionAppType(string appTypeName)
        {
            await _clusterManager.UnprovisionType(appTypeName);
            return Ok(new {result = $"{appTypeName} unprovisioned successfully"});
        }
    }
}
