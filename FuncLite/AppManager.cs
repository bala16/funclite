using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FuncLite
{
    public class AppManager
    {
        readonly HttpClient _client;
        readonly MyConfig _config;
        readonly Queue<App> _freeApps = new Queue<App>();

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

            WarmUpFreeApps().Wait();
        }

        private async Task Init()
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
                foreach (var appProps in json.value)
                {
                    var app = new App(_client, appProps.properties);
                    _freeApps.Enqueue(app);
                }
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

        async Task WarmUpFreeApps()
        {
            var appRefreshTasks = new List<Task>();
            foreach (var app in _freeApps)
            {
                appRefreshTasks.Add(app.SendWarmUpRequest());
            }

            await Task.WhenAll(appRefreshTasks);
        }
    }
}
