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

        // POST api/functions/foo
        [HttpPost("{name}")]
        public async Task<IActionResult> PostFunction([FromRoute] string name, IFormCollection formData)
        {
            var imageUrl = formData.FirstOrDefault(kvp => kvp.Key.Equals("image")).Value.FirstOrDefault();
            var composeFile = formData.Files.FirstOrDefault(f => f.FileName.EndsWith(".yml"));

            if (imageUrl == null || composeFile == null)
            {
                return BadRequest();
            }

            var streamContent = new StreamContent(composeFile.OpenReadStream());
            var composeFileContent = await streamContent.ReadAsStringAsync();
            var composeApplication = new ComposeApplication(name, composeFileContent);
            await _clusterManager.CreateApplication(imageUrl, composeApplication);

            return Ok(new { result = $"Application {name} created"});
        }

        // POST api/functions/foo/run
        [HttpPost]
        [HttpGet]
        [Route("{functionName}/run")]
        public async Task<dynamic> RunFunction(string functionName, string name)
        {
            try
            {
                return await _clusterManager.RunFunction(functionName, name);
            }
            catch (Exception e)
            {
                return StatusCode(400, e.Message);
            }
        }

        // DELETE api/functions/foo
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
    }
}
