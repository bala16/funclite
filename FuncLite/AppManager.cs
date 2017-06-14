using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FuncLite
{
    public class AppManager
    {
        readonly HttpClient _client;
        readonly MyConfig _config;
        readonly Queue<App> _freeApps = new Queue<App>();
        readonly Queue<App> _appsToMarkAsInUse = new Queue<App>();

        public AppManager(IOptions<MyConfig> config)
        {
            _config = config.Value;

            string token = AuthenticationHelpers.AcquireTokenBySPN(
                _config.TenantId, _config.ClientId, _config.ClientSecret).Result;

            _client = new HttpClient(new LoggingHandler(new HttpClientHandler()));
            _client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            _client.BaseAddress = new Uri("https://management.azure.com/");

            Init().Wait();

            CreateNewAppsIfNeeded().Wait();

            new Timer(_ => BackgrounMaintenance().Wait(), null, 0, 60000);
        }

        public App GetApp()
        {
            if (_freeApps.Count == 0)
            {
                throw new Exception("There are no available function workers!");
            }

            var app = _freeApps.Dequeue();

            _appsToMarkAsInUse.Enqueue(app);

            return app;
        }

        async Task Init()
        {
            await CreateResourceGroup(_config.ResourceGroup);

            await LoadAllApps();
        }

        async Task CreateResourceGroup(string resourceGroup)
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

        async Task LoadAllApps()
        {
            using (var response = await _client.GetAsync(
                $"/subscriptions/{_config.Subscription}/resourceGroups/{_config.ResourceGroup}/providers/Microsoft.Web/sites?api-version=2016-03-01"))
            {
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsAsync<dynamic>();

                // Following code is quite ugly. Idea is to delete any site that was previously in use to only start with clean ones
                var inUseAppTasks = new List<Task<App>>();
                foreach (var appProps in json.value)
                {
                    var app = new App(_client, _config, appProps.properties);

                    inUseAppTasks.Add(app.GetInUseState());
                }

                var deleteTasks = new List<Task>();
                foreach (var app in await Task.WhenAll(inUseAppTasks))
                {
                    if (app.InUse)
                    {
                        deleteTasks.Add(app.Delete());
                    }
                    else
                    {
                        _freeApps.Enqueue(app);
                    }
                }

                await Task.WhenAll(deleteTasks);
            }
        }

        async Task CreateNewAppsIfNeeded()
        {
            int neededApps = _config.FreeAppQueueSize - _freeApps.Count;

            var newAppTasks = new List<Task<App>>();
            for (int i=0; i < neededApps; i++)
            {
                newAppTasks.Add(App.CreateApp(_client, _config, "funclite-" + Guid.NewGuid().ToString()));
            }

            foreach (var app in await Task.WhenAll(newAppTasks))
            {
                _freeApps.Enqueue(app);
            }
        }

        async Task BackgrounMaintenance()
        {
            try
            {
                var tasksmarkInUseTasks = new List<Task>();
                while (_appsToMarkAsInUse.Count > 0)
                {
                    tasksmarkInUseTasks.Add(_appsToMarkAsInUse.Dequeue().MarkAsUsed());
                }
                await Task.WhenAll(tasksmarkInUseTasks);

                // Create new apps if needed
                await CreateNewAppsIfNeeded();

                // Warm up all the apps in the free queue
                await WarmUpFreeApps();
            }
            catch (Exception e)
            {
                // Ignore background task exceptions
            }
        }

        async Task WarmUpFreeApps()
        {
            // Keep all the free apps warm
            var appRefreshTasks = new List<Task>();
            foreach (var app in _freeApps)
            {
                appRefreshTasks.Add(app.SendWarmUpRequests());
            }

            await Task.WhenAll(appRefreshTasks);
        }
    }
}
