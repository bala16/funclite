using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FuncLite.Controllers
{
    [Route("api/[controller]")]
    public class ImagesController : Controller
    {
        private readonly ClusterManager _clusterManager;

        public ImagesController(ClusterManager clusterManager)
        {
            _clusterManager = clusterManager;
        }

        [HttpGet]
        public async Task<dynamic> GetRootContents()
        {
            return await _clusterManager.GetRootContents();
        }

        // GET api/images/<contentPath>
        [HttpGet("{contentPath}")]
        public async Task<dynamic> GetContents(string contentPath)
        {
            return await _clusterManager.GetContents(contentPath);
        }

        // POST api/images
        [HttpPost]
        public async Task<IActionResult> UploadFolder(IFormCollection formData)
        {
            var imageRoot = formData.FirstOrDefault(kvp => kvp.Key.Equals("ImagePath")).Value.FirstOrDefault();
            if (imageRoot == null)
            {
                return BadRequest();
            }

            var contentRoot = Path.GetFileName(imageRoot);
            await _clusterManager.UploadFolder(contentRoot, imageRoot);
            return Ok(new {result = $"{imageRoot} uploaded successfully"});
        }

        // DELETE api/images/<contentPath>
        [HttpDelete("{contentPath}")]
        public async Task<IActionResult> DeleteFile(string contentPath)
        {
            await _clusterManager.DeleteFile(contentPath);
            return Ok(new {result = $"{contentPath} deleted successfully"});
        }
    }
}
