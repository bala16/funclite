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
        private const int AppScaleDownThreshold = 0;
        private const int AppScaleUpThreshold = 4;
        private const int AppMonitoringInterval = -60;
        private readonly HttpClient _client;
        private readonly HttpClient _functionsHttpClient;
        private readonly MyConfig _config;

        private readonly SemaphoreSlim _lock;
        private readonly SemaphoreSlim _backgroundTaskLock;
        private readonly ConcurrentDictionary<string, ContainerGroupCollection> _containerGroupCollections;
        private readonly ConcurrentDictionary<string, List<long>> _appUsageTimes;
        private readonly Timer _timer;

        public ILogger<ACIManager> Logger { get; }

        public ACIManager(IOptions<MyConfig> config, ILogger<ACIManager> logger)
        {
            _config = config.Value;
            Logger = logger;

            _lock = new SemaphoreSlim(1, 1);
            _backgroundTaskLock = new SemaphoreSlim(1,1);
            _containerGroupCollections = new ConcurrentDictionary<string, ContainerGroupCollection>(StringComparer.OrdinalIgnoreCase);
            _appUsageTimes = new ConcurrentDictionary<string, List<long>>(StringComparer.OrdinalIgnoreCase);

            string token = AuthenticationHelpers.AcquireTokenBySPN(
                _config.TenantId, _config.ClientId, _config.ClientSecret).Result;

            _client = new HttpClient(new LoggingHandler(new HttpClientHandler(), logger));
            _client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            _client.BaseAddress = new Uri("https://management.azure.com/");

            _functionsHttpClient = new HttpClient();

            LoadExistingApps().Wait();

            _timer = new Timer(async _ => await BackgroundMaintenance(), null, 0, 60000); // 10 seconds
        }

        private async Task BackgroundMaintenance()
        {
            var startTask = _backgroundTaskLock.Wait(0);
            try
            {
                if (startTask)
                {
                    var appNames = new List<string>(_containerGroupCollections.Keys);
                    var appsToScaleUp = new List<string>();
                    var appsToScaleDown = new List<string>();
                    var interval = DateTime.Now.AddSeconds(AppMonitoringInterval).Ticks;

                    foreach (var appName in appNames)
                    {
                        if (!_appUsageTimes.ContainsKey(appName)) continue;
                        var appUsageTimes = _appUsageTimes[appName];
                        var index = 0;
                        while (index < appUsageTimes.Count && appUsageTimes[index] < interval)
                        {
                            index++;
                        }
                        // todo Remove execution times before start of interval period

                        var executionsInLastInterval = appUsageTimes.Count - index;

                        var containerGroups = _containerGroupCollections[appName];
                        if (executionsInLastInterval <= AppScaleDownThreshold && containerGroups.CanScaleDown())
                        {
                            appsToScaleDown.Add(appName);
                        }
                        else if (executionsInLastInterval > AppScaleUpThreshold && containerGroups.CanScaleUp())
                        {
                            _appUsageTimes[appName] = new List<long>();
                            appsToScaleUp.Add(appName);
                        }
                    }

                    var scaleTasks = appsToScaleUp.Select(appName => ScaleApp(appName, true)).ToList();
                    Logger.LogInformation($"Scale Up task count: {scaleTasks.Count}");
                    var scaleDownTasks = appsToScaleDown.Select(appName => ScaleApp(appName, false)).ToList();
                    Logger.LogInformation($"Scale down task count: {scaleDownTasks.Count}");
                    scaleTasks.AddRange(scaleDownTasks);

                    await Task.WhenAll(scaleTasks);
                }
                else
                {
                    Logger.LogInformation("Skipping background maintenance");
                }

            }
            catch (Exception)
            {
                // Ignore
            }
            finally
            {
                if (startTask)
                {
                    _backgroundTaskLock.Release();
                }
            }
        }

        public async Task LoadExistingApps()
        {
            await _lock.WaitAsync();
            try
            {
                var jsonBody = await GetContainerGroups();
                JArray valueArray = jsonBody.value;
                var publicPorts = new HashSet<uint>();
                foreach (dynamic value in valueArray)
                {
                    string containerGroupName = value.name;
                    string appName = ACIUtils.GetAppNameFromContainerGroupName(containerGroupName);
                    var properties = value.properties;
                    var ipAddress = properties.ipAddress;
                    string ip = ipAddress.ip;
                    if (ip == null) continue;
                    var containerGroup = new ContainerGroup(containerGroupName, _config.Region, ip);
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
                    ContainerGroupCollection containerGroupCollection;
                    if (!_containerGroupCollections.TryGetValue(appName, out containerGroupCollection))
                    {
                        containerGroupCollection = new ContainerGroupCollection(appName);
                    }
                    containerGroupCollection.AddContainerGroup(containerGroup);
                    _containerGroupCollections[appName] = containerGroupCollection;
                    publicPorts.Clear();
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public dynamic GetContainerGroups(string appName)
        {
            ContainerGroupCollection containerGroupCollection;
            if (_containerGroupCollections.TryGetValue(appName, out containerGroupCollection))
            {
                return containerGroupCollection.GetIpAddresses();
            }
            return new JObject();
        }

        private async Task<dynamic> GetContainerGroups()
        {
            using (var response = await _client.GetAsync(
                $"/subscriptions/{_config.Subscription}/resourceGroups/{_config.ResourceGroup}/providers/Microsoft.ContainerInstance/containerGroups?api-version=2017-08-01-preview")
            )
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsAsync<dynamic>();
            }
        }

        private void RegisterAppUsage(string appName)
        {
            var appUsageTimes = _appUsageTimes.ContainsKey(appName) ? _appUsageTimes[appName] : new List<long>();
            appUsageTimes.Add(DateTime.Now.Ticks);
            Logger.LogInformation($"{appName} triggered at {appUsageTimes.Last()}");
            _appUsageTimes[appName] = appUsageTimes;
        }

        public async Task<dynamic> RunFunction(string appName, string functionName, string name)
        {
            ContainerGroupCollection appContainerGroupCollection;
            if (!_containerGroupCollections.TryGetValue(appName, out appContainerGroupCollection))
            {
                throw new Exception($"{appName} containerGroup not found");
            }

            RegisterAppUsage(appName);

            var nextContainerGroup = appContainerGroupCollection.GetNextContainerGroup();
            var ipAddress = nextContainerGroup.IpAddress;
            var port = nextContainerGroup.PublicPorts.First();
            var functionEndpoint = $"http://{ipAddress}:{port}/api/{functionName}/?name={name}";
            Logger.LogInformation($"Triggering function {appName}/{functionName} at {nextContainerGroup.Name}");

            using (var response = await _functionsHttpClient.GetAsync(functionEndpoint))
            {
                if (!response.IsSuccessStatusCode)
                {
                    Logger.LogError($"{nextContainerGroup.Name} failed with {response.StatusCode}");
                }
                else
                {
                    Logger.LogInformation($"{nextContainerGroup.Name} succeeded with {response.StatusCode}");
                }
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }

        public IEnumerable<string> GetApps()
        {
            return _containerGroupCollections.Keys;
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

        private async Task<string> CreateContainerGroup(ContainerGroup containerGroup)
        {
            using (var response = await _client.PutAsJsonAsync(
                $"/subscriptions/{_config.Subscription}/resourceGroups/{_config.ResourceGroup}/providers/Microsoft.ContainerInstance/containerGroups/{containerGroup.Name}?api-version=2017-08-01-preview",
                containerGroup.ToACICreateContainerRequest()))
            {
                response.EnsureSuccessStatusCode();
                return await GetIpAddressFromARM(containerGroup.Name);
            }
        }

        public async Task CreateApp(string appName, dynamic appDefinition)
        {
            await _lock.WaitAsync();
            try
            {
                if (_containerGroupCollections.ContainsKey(appName))
                {
                    throw new Exception($"{appName} exists already");
                }

                var containerGroupDefinition = appDefinition.containerGroup;
                var containerDefinitions = containerGroupDefinition.containers;

                var containerGroupName = ACIUtils.GetContainerGroupNameForAppName(appName);
                var containerGroup = new ContainerGroup(containerGroupName, _config.Region, "");
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

                containerGroup.IpAddress = await CreateContainerGroup(containerGroup);
                var containerGroupCollection = new ContainerGroupCollection(appName);
                containerGroupCollection.AddContainerGroup(containerGroup);
                _containerGroupCollections[appName] = containerGroupCollection;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<string> GetIpAddressFromARM(string appName)
        {
            using (var response = await _client.GetAsync(
                $"/subscriptions/{_config.Subscription}/resourceGroups/{_config.ResourceGroup}/providers/Microsoft.ContainerInstance/containerGroups/{appName}?api-version=2017-08-01-preview")
            )
            {
                response.EnsureSuccessStatusCode();
                var containerGroupInfo = await response.Content.ReadAsAsync<dynamic>();
                var properties = containerGroupInfo.properties;
                var ipAddress = properties.ipAddress;
                return ipAddress.ip;
            }
        }

        private async Task DeleteContainerGroup(string containerGroupName)
        {
            using (var response = await _client.DeleteAsync(
                    $"/subscriptions/{_config.Subscription}/resourceGroups/{_config.ResourceGroup}/providers/Microsoft.ContainerInstance/containerGroups/{containerGroupName}?api-version=2017-08-01-preview")
            )
            {
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task DeleteApp(string appName)
        {
            await _lock.WaitAsync();
            try
            {
                if (!_containerGroupCollections.ContainsKey(appName))
                {
                    throw new Exception($"{appName} not found");
                }
                ContainerGroupCollection containerGroupCollection;
                if (_containerGroupCollections.TryRemove(appName, out containerGroupCollection))
                {
                    var deleteContainerGroupsTask = containerGroupCollection.GetContainerGroupNames()
                        .Select(DeleteContainerGroup);
                    await Task.WhenAll(deleteContainerGroupsTask);

                    List<long> appUsage;
                    _appUsageTimes.TryRemove(appName, out appUsage);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task DelayAddContainerGroup(string appName, ContainerGroupCollection containerGroupCollection, ContainerGroup containerGroup)
        {
            await Task.Delay(15000);
            containerGroupCollection.AddContainerGroup(containerGroup);
            _containerGroupCollections[appName] = containerGroupCollection;
        }

        public async Task ScaleApp(string appName, bool up)
        {
            ContainerGroupCollection containerGroupCollection;
            if (_containerGroupCollections.TryGetValue(appName, out containerGroupCollection))
            {
                if (up)
                {
                    var nextContainerGroup = containerGroupCollection.GetNextContainerGroup();
                    var duplicateContainerGroup = nextContainerGroup.Duplicate();
                    duplicateContainerGroup.IpAddress = await CreateContainerGroup(duplicateContainerGroup);
                    //todo Add delay to let containers start
                    await DelayAddContainerGroup(appName, containerGroupCollection, duplicateContainerGroup);
                }
                else
                {
                    var containerGroup = containerGroupCollection.RemoveNextContainerGroup();
                    await DeleteContainerGroup(containerGroup.Name);
                    _containerGroupCollections[appName] = containerGroupCollection;
                }
            }
        }
    }
}