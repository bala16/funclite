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
        public string Get()
        {
            return "AppsController started";
        }

        // POST api/apps/foo
        [HttpPost("{name}")]
        public async Task<IActionResult> CreateApp([FromRoute] string name, IFormCollection formData)
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

            return Ok(new { result = $"Application {name} created" });
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

    }
}