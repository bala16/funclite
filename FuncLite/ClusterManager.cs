using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace FuncLite
{
    public class ClusterManager
    {
        private readonly MyConfig _config;
        private readonly string _endPoint;
        private readonly int _portNumber;
        readonly HttpClient _client;

        private static readonly string ClusterEndpointTemplate = "{0}:{1}";

        public ClusterManager(IOptions<MyConfig> config, ILogger<AppManager> logger)
        {
            _config = config.Value;
            _endPoint = _config.ClusterEndPoint;
            _portNumber = int.Parse(this._config.ClusterPort);

            _client = new HttpClient(new LoggingHandler(new HttpClientHandler(), logger));
            _client.BaseAddress = new Uri(string.Format(ClusterEndpointTemplate, _endPoint, _portNumber));
        }

        public async Task<dynamic> GetApplications()
        {
            using (var response = await _client.GetAsync(
                $"/Applications?api-version=3.0"))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsAsync<dynamic>();
            }
        }

        // http://localhost:51454/api/services/App1/FunctionsService1/
        public async Task CreateService(string appName, string serviceName, int instanceCount)
        {
            var jsonBody = new
            {
                ServiceKind = "Stateless",
                ServiceName = $"fabric:/{appName}/{serviceName}-"+Guid.NewGuid(),
                ServiceTypeName = $"{serviceName}Type",
                PartitionDescription = new
                {
                    PartitionScheme = "Singleton"
                },
                InstanceCount = instanceCount
            };
            using (var response = await _client.PostAsJsonAsync(
                $"/Applications/{appName}/$/GetServices/$/Create?api-version=3.0", jsonBody))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task UpdateService(string appName, string serviceName, int instanceCount)
        {
            var serviceId = $"{appName}/{serviceName}";
            using (var response = await _client.PostAsJsonAsync(
                $"/Services/{serviceId}/$/Update?api-version=3.0",
                new
                {
                    ServiceKind = "Stateless",
                    InstanceCount = instanceCount
                }))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task CreateComposeApp(ComposeApplication composeApplication)
        {
            using (var response = await _client.PutAsJsonAsync(
                $"/ComposeDeployments/$/Create?api-version=4.0-preview&timeout=60",
                new
                {
                    ApplicationName = composeApplication.ApplicationName,
                    ComposeFileContent = composeApplication.ComposeFileContent,
                }))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task CreateApp(string appName, string appTypeName, string appTypeVersion)
        {
            var applicationName = $"fabric:/{appName}";
            using (var response = await _client.PostAsJsonAsync(
                $"/Applications/$/Create?api-version=3.0",
                new
                {
                    Name = applicationName,
                    TypeName = appTypeName,
                    TypeVersion = appTypeVersion
                }))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task<dynamic> ExecuteFunction(string appName, string serviceName, string functionName, string name)
        {
            var functionEndpoint = $"{_config.ClusterEndPoint}/api/{appName}/{serviceName}/{functionName}?name={name}";
            using (var functionHttpClient = new HttpClient())
            {
                using (var response = await functionHttpClient.GetAsync(functionEndpoint))
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync();
                }
            }
        }

        public async Task DeleteApplication(string appName)
        {
            using (var response = await _client.PostAsync($"/Applications/{appName}/$/Delete?api-version=3.0", null))
            {
                response.EnsureSuccessStatusCode();
            }
        }


        public async Task DeleteFile(string contentPath)
        {
            using (var respoonse = await _client.DeleteAsync($"/ImageStore/{contentPath}?api-version=3.0"))
            {
                respoonse.EnsureSuccessStatusCode();
            }
        }

        public async Task<dynamic> GetContents(string contentPath)
        {
            using (var response = await _client.GetAsync(
                $"/ImageStore/{contentPath}?api-version=3.0"))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsAsync<dynamic>();
            }
        }

        public async Task<dynamic> GetRootContents()
        {
            using (var response = await _client.GetAsync(
                $"/ImageStore?api-version=3.0"))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsAsync<dynamic>();
            }
        }

        public async Task<dynamic> GetApplicationTypes()
        {
            using (var response = await _client.GetAsync(
                $"/ApplicationTypes?api-version=4.0"))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsAsync<dynamic>();
            }
        }

        public async Task<dynamic> GetApplicationTypeInfo(string appTypeName)
        {
            using (var response = await _client.GetAsync(
                $"/ApplicationTypes/{appTypeName}?api-version=4.0"))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsAsync<dynamic>();
            }
        }

        public async Task ProvisionAppType(string imagePath)
        {
            using (var response = await _client.PostAsJsonAsync($"/ApplicationTypes/$/Provision?api-version=3.0", 
                new
                {
                    ApplicationTypeBuildPath = imagePath
                }))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task UnprovisionType(string appTypeName)
        {
            using (var response = await _client.PostAsJsonAsync($"/ApplicationTypes/{appTypeName}/$/Unprovision?api-version=3.0",
                new
                {
                    ApplicationTypeVersion = "1.0.0"
                }))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task<dynamic> GetApplicationInfo(string appName)
        {
            using (var response = await _client.GetAsync(
                $"/Applications/{appName}?api-version=3.0"))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsAsync<dynamic>();
            }

        }

        private async Task UploadFile(string contentPath, string contentFileContents)
        {
            using (var response = await _client.PutAsync($"/ImageStore/{contentPath}?api-version=3.0",
                new StringContent(contentFileContents)))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        private async Task Upload(string contentRoot, string filePath)
        {
            var contentPath = $"{contentRoot}/{Path.GetFileName(filePath)}";
            using (var response = await _client.PutAsync($"/ImageStore/{contentPath}?api-version=3.0",
                new StringContent(File.ReadAllText(filePath))))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task UploadFolder(string contentRoot, string imageRoot)
        {
            foreach (var file in Directory.EnumerateFiles(imageRoot))
            {
                await Upload(contentRoot, file);
            }

            foreach (var directory in Directory.EnumerateDirectories(imageRoot))
            {
                await UploadFolder($"{contentRoot}/{Path.GetFileName(directory)}", directory);
            }
        }

        public async Task<dynamic> GetServices(string appId)
        {
            using (var response = await _client.GetAsync(
                $"/Applications/{appId}/$/GetServices/?api-version=3.0"))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsAsync<dynamic>();
            }
        }

        public async Task<dynamic> GetServiceInfo(string appId, string serviceId)
        {
            string fullServiceId = $"{appId}/{serviceId}";
            using (var response = await _client.GetAsync(
                $"/Applications/{appId}/$/GetServices/{fullServiceId}?api-version=3.0"))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsAsync<dynamic>();
            }
        }

        public async Task DeleteService(string appId, string serviceId)
        {
            string fullServiceId = $"{appId}/{serviceId}";
            using (var response = await _client.PostAsync(
                $"/Services/{fullServiceId}/$/Delete?api-version=3.0", null))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task DeleteComposeApp(string appName)
        {
            using (var response = await _client.PostAsync(
                $"/ComposeDeployments/{appName}/$/Delete?api-version=4.0-preview", null))
            {
                response.EnsureSuccessStatusCode();
            }

        }
    }
}
