using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FuncLite
{
    public class ServiceFabricAppManager
    {
        private readonly ClusterManager _clusterManager;
        private readonly Dictionary<string, ServiceFabricApp> _sfApps;
        private uint _nextAvailablePort;

        public ServiceFabricAppManager(ClusterManager clusterManager)
        {
            _clusterManager = clusterManager;
            _sfApps = new Dictionary<string, ServiceFabricApp>(StringComparer.OrdinalIgnoreCase);
            _nextAvailablePort = 81;

//            Init().Wait();
        }

        private async Task Init()
        {
            await LoadExistingApps();
        }

        private async Task LoadExistingApps()
        {
            var appsJson = await _clusterManager.GetApplications();
            foreach (var item in appsJson.Items)
            {
                string key = Convert.ToString(item.Id);
                if (!_sfApps.ContainsKey(key))
                {
                    _sfApps[key] = new ServiceFabricApp(key, $"{key}Service");
                }
            }
        }

        public Dictionary<string, ServiceFabricApp>.KeyCollection GetApps()
        {
            return _sfApps.Keys;
        }

        public ServiceFabricApp GetApp(string appName)
        {
            return _sfApps.ContainsKey(appName) ? _sfApps[appName] : null;
        }

        // creating app with name f1 => apptype = Compose_f1
        // create app with name f1 (fabric:/f1)
        // service name is from docker compose file
        public async Task CreateApp(string appName, string imagePath, int replicaCount)
        {
            var yaml = $"version: \"3\"\r\nservices:\r\n    {appName}Service:\r\n        image: {imagePath}\r\n        ports:\r\n             - \"{_nextAvailablePort++}:1337/http\"\r\n        deploy:\r\n            replicas: {replicaCount}";
            var composeApplication = new ComposeApplication(appName, yaml);
            await _clusterManager.CreateComposeApp(composeApplication);
            //todo add locks
            _sfApps[appName] = new ServiceFabricApp(appName, $"{appName}Service");
        }

        public async Task DeleteApp(string appName)
        {
            await _clusterManager.DeleteComposeApp(appName);
            _sfApps.Remove(appName);
        }

        public async Task<dynamic> ExecuteFunction(string appName, string functionName, string name)
        {
            var serviceFabricApp = GetApp(appName);
            return await _clusterManager.ExecuteFunction(serviceFabricApp.AppName, serviceFabricApp.ServiceName, functionName, name);
        }
    }
}
