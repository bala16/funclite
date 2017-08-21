using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace FuncLite.Controllers
{
    [Route("api/[controller]")]
    public class ServicesController : Controller
    {
        private readonly ClusterManager _clusterManager;

        public ServicesController(ClusterManager clusterManager)
        {
            _clusterManager = clusterManager;
        }

        [HttpGet("{appId}")]
        public async Task<dynamic> GetServices(string appId)
        {
            return await _clusterManager.GetServices(appId);
        }

        [HttpGet("{appId}/{serviceId}")]
        public async Task<dynamic> GetServiceInfo(string appId, string serviceId)
        {
            return await _clusterManager.GetServiceInfo(appId, serviceId);
        }

        [HttpDelete("{appId}/{serviceId}")]
        public async Task<IActionResult> DeleteService(string appId, string serviceId)
        {
            await _clusterManager.DeleteService(appId, serviceId);
            return Ok($"{appId}/{serviceId} deleted successfully");
        }

        [HttpPost("{appId}/{serviceId}/{instanceCount}")]
        public async Task<IActionResult> CreateService(string appId, string serviceId, int instanceCount)
        {
            await _clusterManager.CreateService(appId, serviceId, instanceCount);
            return Ok($"{appId}/{serviceId} created with {instanceCount} instances");
        }

        // POST api/services/<appName</<serviceName>/<instanceCount>/scale
        [HttpPost("{appName}/{serviceName}/{instanceCount}/scale")]
        public async Task<IActionResult> UpdateService(string appName, string serviceName, int instanceCount)
        {
            await _clusterManager.UpdateService(appName, serviceName, instanceCount);
            return Ok($"{appName}/{serviceName} scaled to {instanceCount} instances");
        }

    }
}
