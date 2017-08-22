using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace FuncLite
{
    public class ACIManager
    {
        private readonly HttpClient _client;
        private readonly MyConfig _config;
        public ACIManager(IOptions<MyConfig> config, ILogger<ACIManager> logger)
        {
            _config = config.Value;

            string token = AuthenticationHelpers.AcquireTokenBySPN(
                _config.TenantId, _config.ClientId, _config.ClientSecret).Result;

            _client = new HttpClient(new LoggingHandler(new HttpClientHandler(), logger));
            _client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            _client.BaseAddress = new Uri("https://management.azure.com/");

//            CreateResourceGroup(_config.ResourceGroup).Wait();
        }

        private async Task CreateResourceGroup(string resourceGroup)
        {
            using (var response = await _client.PutAsJsonAsync(
                $"/subscriptions/{_config.Subscription}/resourceGroups/{resourceGroup}?api-version=2015-11-01",
                new
                {
                    location = _config.Region
                }))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task<dynamic> GetContainerGroups()
        {
            using (var response = await _client.GetAsync(
                $"/subscriptions/{_config.Subscription}/resourceGroups/{_config.ResourceGroup}/providers/Microsoft.ContainerInstance/containerGroups?api-version=2017-08-01-preview"))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsAsync<dynamic>();
            }
        }

        public async Task<dynamic> GetContainerGroup(string containerGroupName)
        {
            using (var response = await _client.GetAsync(
                $"/subscriptions/{_config.Subscription}/resourceGroups/{_config.ResourceGroup}/providers/Microsoft.ContainerInstance/containerGroups/{containerGroupName}?api-version=2017-08-01-preview"))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsAsync<dynamic>();
            }
        }

        public async Task CreateContainerGroup(string containerGroupName, dynamic definition)
        {
            var containerItem = new
            {
                name = containerGroupName,
                properties = new
                {
                    image = "balag0/functionsproxy:v1",
                    ports = new [] { new  { port = 7331 } },
                    resources = new
                    {
                        requests = new
                        {
                            memoryInGb = 1.5,
                            cpu = 1.0
                        }
                    }
                }
            };

            var ipAddressPorts = new[]
            {
                new
                {
                    protocol = "TCP",
                    port = 7331
                }
            };

            var containerGroup = new
            {
                location = _config.Region,
                properties = new
                {
                    osType = "Linux",
                    ipAddress = new { ports = ipAddressPorts, type = "Public" },
                    containers = new [] {containerItem}
                }
            };

            using (var response = await _client.PutAsJsonAsync(
                $"/subscriptions/{_config.Subscription}/resourceGroups/{_config.ResourceGroup}/providers/Microsoft.ContainerInstance/containerGroups/{containerGroupName}?api-version=2017-08-01-preview",
                containerGroup))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task DeleteContainerGroup(string containerGroupName)
        {
            using (var respoonse = await _client.DeleteAsync($"/subscriptions/{_config.Subscription}/resourceGroups/{_config.ResourceGroup}/providers/Microsoft.ContainerInstance/containerGroups/{containerGroupName}?api-version=2017-08-01-preview"))
            {
                respoonse.EnsureSuccessStatusCode();
            }
        }

        public async Task<dynamic> GetContainerLogs(string containerGroupName, string containerName)
        {
            using (var response = await _client.GetAsync(
                $"/subscriptions/{_config.Subscription}/resourceGroups/{_config.ResourceGroup}/providers/Microsoft.ContainerInstance/containerGroups/{containerGroupName}/containers/{containerName}/logs?api-version=2017-08-01-preview"))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsAsync<dynamic>();
            }
        }
    }
}
