using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FuncLite.ACIModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace FuncLite
{
    public class ACIManager
    {
        private readonly HttpClient _client;
        private readonly MyConfig _config;

        private readonly ReaderWriterLockSlim _lock;
        private readonly ConcurrentDictionary<string, ContainerGroup> _containerGroups;

        public ACIManager(IOptions<MyConfig> config, ILogger<RawACIManager> logger)
        {
            _config = config.Value;

            _lock = new ReaderWriterLockSlim();
            _containerGroups = new ConcurrentDictionary<string, ContainerGroup>();

            string token = AuthenticationHelpers.AcquireTokenBySPN(
                _config.TenantId, _config.ClientId, _config.ClientSecret).Result;

            _client = new HttpClient(new LoggingHandler(new HttpClientHandler(), logger));
            _client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            _client.BaseAddress = new Uri("https://management.azure.com/");

            LoadExistingApps().Wait();
        }

        private async Task<dynamic> GetContainerGroups()
        {
            using (var response = await _client.GetAsync(
                $"/subscriptions/{_config.Subscription}/resourceGroups/{_config.ResourceGroup}/providers/Microsoft.ContainerInstance/containerGroups?api-version=2017-08-01-preview"))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsAsync<dynamic>();
            }
        }

        public async Task LoadExistingApps()
        {
            var jsonBody = await GetContainerGroups();
            JArray valueArray = jsonBody.value;
            var publicPorts = new HashSet<uint>();
            foreach (dynamic value in valueArray)
            {
                string containerGroupName = value.name;
                var containerGroup = new ContainerGroup(containerGroupName, _config.Region);
                var properties = value.properties;
                var ipAddress = properties.ipAddress;
                string ip = ipAddress.ip;
                containerGroup.IpAddress = ip;
                foreach (var port in ipAddress.ports)
                {
                    uint portNo = port.port;
                    publicPorts.Add(portNo);
                }
                var containers = properties.containers;
                foreach (var container in containers)
                {
                    string containerName = container.name;
                    var containerProperties = container.properties;
                    string containerImage = containerProperties.image;
                    var currentContainer = new Container(containerName, containerImage);
                    var containerPorts = containerProperties.ports;
                    foreach (var containerPort in containerPorts)
                    {
                        uint portNo = containerPort.port;
                        var cPort = new ContainerPort(portNo, publicPorts.Contains(portNo));
                        currentContainer.AddPort(cPort);
                    }
                    containerGroup.AddContainer(currentContainer);
                }
                _containerGroups.TryAdd(containerGroupName, containerGroup);
                AddApp(containerGroupName, containerGroup);
                publicPorts.Clear();
            }
        }

        public async Task<dynamic> RunFunction(string appName, string functionName, string name)
        {
            ContainerGroup appContainerGroup;
            if (!_containerGroups.TryGetValue(appName, out appContainerGroup))
            {
                throw new Exception($"{appName} containerGroup not found");
            }

            var ipAddress = appContainerGroup.IpAddress;
            var port = appContainerGroup.PublicPorts.First();

            var functionEndpoint = $"http://{ipAddress}:{port}/api/{functionName}/?name={name}";

            using (var functionHttpClient = new HttpClient())
            {
                using (var response = await functionHttpClient.GetAsync(functionEndpoint))
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync();
                }
            }
        }

        public IEnumerable<string> GetApps()
        {
            return _containerGroups.Keys;
        }

        private bool AppExists(string appName)
        {
            return _containerGroups.ContainsKey(appName);
        }

        private void AddApp(string appName, ContainerGroup containerGroup)
        {
            _containerGroups.TryAdd(appName, containerGroup);
        }

        public async Task<List<ACIApp>> GetAppsFromARM()
        {
            var aciApps = new List<ACIApp>();
            var containerGroups = await GetContainerGroups();

            foreach (var containerGroup in containerGroups.value)
            {
                var containerProperties = containerGroup.properties.containers[0].properties;
                string image = containerProperties.image;
                var aciApp = new ACIApp(image);
                aciApps.Add(aciApp);
            }
            return aciApps;
        }

        public async Task CreateApp(string appName, dynamic appDefinition)
        {
            if (!_containerGroups.TryAdd(appName, null))
            {
                throw new Exception($"{appName} exists already");
            }

            var containerGroupDefinition = appDefinition.containerGroup;
            var containerDefinitions = containerGroupDefinition.containers;

            var containerGroup = new ContainerGroup(appName, _config.Region);
            foreach (var containerDefinition in containerDefinitions)
            {
                string containerName = containerDefinition.name;
                string image = containerDefinition.image;
                var container = new Container(containerName, image);
                var containerPortsDefinition = containerDefinition.ports;

                foreach (var containerPortDefinition in containerPortsDefinition)
                {
                    uint port = containerPortDefinition.port;
                    string typeString = containerPortDefinition.type;
                    var isPublic = typeString.Equals("public", StringComparison.OrdinalIgnoreCase);
                    var containerPort = new ContainerPort(port, isPublic);
                    container.AddPort(containerPort);
                }
                containerGroup.AddContainer(container);
            }

            
            var aciCreateContainerRequest = containerGroup.ToACICreateContainerRequest();

            using (var response = await _client.PutAsJsonAsync(
                $"/subscriptions/{_config.Subscription}/resourceGroups/{_config.ResourceGroup}/providers/Microsoft.ContainerInstance/containerGroups/{appName}?api-version=2017-08-01-preview",
                aciCreateContainerRequest))
            {
                response.EnsureSuccessStatusCode();
                var ipAddress = await GetIpAddressFromARM(appName);
                containerGroup.IpAddress = ipAddress;
                _containerGroups.TryUpdate(appName, containerGroup, null);
            }
        }

        public async Task<string> GetIpAddressFromARM(string appName)
        {
            using (var response = await _client.GetAsync(
                $"/subscriptions/{_config.Subscription}/resourceGroups/{_config.ResourceGroup}/providers/Microsoft.ContainerInstance/containerGroups/{appName}?api-version=2017-08-01-preview"))
            {
                response.EnsureSuccessStatusCode();
                var containerGroupInfo = await response.Content.ReadAsAsync<dynamic>();
                var properties = containerGroupInfo.properties;
                var ipAddress = properties.ipAddress;
                return ipAddress.ip;
            }
        }

        public async Task DeleteApp(string appName)
        {
            if (!AppExists(appName))
            {
                throw new Exception($"{appName} not found");
            }

            using (var response = await _client.DeleteAsync($"/subscriptions/{_config.Subscription}/resourceGroups/{_config.ResourceGroup}/providers/Microsoft.ContainerInstance/containerGroups/{appName}?api-version=2017-08-01-preview"))
            {
                response.EnsureSuccessStatusCode();
                ContainerGroup appContainerGroup;
                _containerGroups.TryRemove(appName, out appContainerGroup);
            }
        }
    }
}
