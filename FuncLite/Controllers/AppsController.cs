using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FuncLite.Controllers
{
    [Route("api/[controller]")]
    public class AppsController : Controller
    {
        private readonly ClusterManager _clusterManager;

        public AppsController(ClusterManager clusterManager)
        {
            _clusterManager = clusterManager;
        }

        [HttpGet]
        public async Task<dynamic> GetAllApps()
        {
            return await _clusterManager.GetApplications();
        }

        [HttpGet("{appName}")]
        public async Task<dynamic> GetApplication(string appName)
        {
            return await _clusterManager.GetApplicationInfo(appName);
        }

        // POST api/apps/<appName>
        [HttpPost("{appName}")]
        public async Task<IActionResult> CreateApp(string appName, IFormCollection formData)
        {
            var appTypeName = formData.FirstOrDefault(kvp => kvp.Key.Equals("TypeName")).Value.FirstOrDefault();
            var appTypeVersion = formData.FirstOrDefault(kvp => kvp.Key.Equals("TypeVersion")).Value.FirstOrDefault();
            if (appTypeName == null || appTypeVersion == null)
            {
                return BadRequest();
            }
            await _clusterManager.CreateApp(appName, appTypeName, appTypeVersion);
            return Ok(new {result = $"Application {appName} created"});
        }

        // DELETE api/apps/foo
        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete(string name)
        {
            try
            {
                await _clusterManager.DeleteApplication(name);
            }
            catch (Exception e)
            {
                return StatusCode(400, e.Message);
            }

            return Ok(new { result = $"Application {name} deleted" });
        }

        // POST api/apps/foo/compose
        [HttpPost("{name}/compose")]
        public async Task<IActionResult> CreateComposeApp([FromRoute] string name, IFormCollection formData)
        {
            var composeFile = formData.Files.FirstOrDefault(f => f.FileName.EndsWith(".yml"));

            if (composeFile == null)
            {
                return BadRequest();
            }

            var streamContent = new StreamContent(composeFile.OpenReadStream());
            var composeFileContent = await streamContent.ReadAsStringAsync();
            var composeApplication = new ComposeApplication(name, composeFileContent);
            await _clusterManager.CreateComposeApp(composeApplication);

            return Ok(new { result = $"Application {name} created" });
        }
    }
}