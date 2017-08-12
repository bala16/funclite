using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Net.Http;

namespace FuncLite.Controllers
{
    [Route("api/[controller]")]
    public class FunctionsController : Controller
    {
        private readonly ClusterManager _clusterManager;

        public FunctionsController(ClusterManager clusterManager)
        {
            _clusterManager = clusterManager;
        }

        [HttpGet]
        public string Get()
        {
            return "FunctionsController started";
        }

        //POST/GET api/functions/<appname>/<servicename>/<functionName>/run
        [HttpGet]
        [HttpPost]
        [Route("{appName}/{serviceName}/{functionName}/run")]
        public async Task<dynamic> RunFunction(string appName, string serviceName, string functionName, string name)
        {
            try
            {
                return await _clusterManager.ExecuteFunction(appName, serviceName, functionName, name);
            }
            catch (Exception e)
            {
                return StatusCode(400, e.Message);
            }
        }
    }
}
