using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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

        private readonly Dictionary<Language, Queue<BaseApp>> _allFreeApps = new Dictionary<Language, Queue<BaseApp>>();
        private readonly Dictionary<Language, Queue<BaseApp>> _allAppsToMarkAsInUse = new Dictionary<Language, Queue<BaseApp>>();

        public AppManager(IOptions<MyConfig> config, ILogger<AppManager> logger)
        {
            _config = config.Value;
            Logger = logger;

            string token = AuthenticationHelpers.AcquireTokenBySPN(
                _config.TenantId, _config.ClientId, _config.ClientSecret).Result;

            _client = new HttpClient(new LoggingHandler(new HttpClientHandler(), logger));
            _client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            _client.BaseAddress = new Uri("https://management.azure.com/");

            _allFreeApps.Add(Language.Node, new Queue<BaseApp>());
            _allFreeApps.Add(Language.Ruby, new Queue<BaseApp>());

            _allAppsToMarkAsInUse.Add(Language.Node, new Queue<BaseApp>());
            _allAppsToMarkAsInUse.Add(Language.Ruby, new Queue<BaseApp>());

            Init().Wait();

            CreateNewAppsIfNeeded().Wait();

            new Timer(_ => BackgrounMaintenance().Wait(), null, 0, 60000);
        }

        public ILogger<AppManager> Logger { get; private set; }

        public BaseApp GetAppFor(Language language)
        {
            var appQueue = _allFreeApps[language];
            if (appQueue.Count == 0)
            {
                throw new Exception("There are no available function workers for " + language);
            }

            var readyApp = appQueue.Dequeue();

            _allAppsToMarkAsInUse[language].Enqueue(readyApp);

            return readyApp;
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
                var allInUseAppTasks = new Dictionary<Language, List<Task<BaseApp>>>();
                allInUseAppTasks.Add(Language.Node, new List<Task<BaseApp>>());
                allInUseAppTasks.Add(Language.Ruby, new List<Task<BaseApp>>());

                foreach (var appProps in json.value)
                {
                    string appName = appProps.properties.name;
                    BaseApp inUseApp;
                    if (appName.StartsWith("Linux"))
                    {
                        inUseApp = new LinuxApp(_client, _config, Logger, appProps.properties);
                        allInUseAppTasks[Language.Ruby].Add(inUseApp.GetInUseState());
                    }
                    else
                    {
                        inUseApp = new WindowsApp(_client, _config, Logger, appProps.properties);
                        allInUseAppTasks[Language.Node].Add(inUseApp.GetInUseState());
                    }
                }

                var allDeleteTasks = new List<Task>();
                foreach (var app in await Task.WhenAll(allInUseAppTasks[Language.Ruby]))
                {
                    if (app.InUse)
                    {
                        allDeleteTasks.Add(app.Delete());
                    }
                    else
                    {
                        _allFreeApps[app.Language].Enqueue(app);
                    }
                }

                foreach (var app in await Task.WhenAll(allInUseAppTasks[Language.Node]))
                {
                    if (app.InUse)
                    {
                        allDeleteTasks.Add(app.Delete());
                    }
                    else
                    {
                        _allFreeApps[app.Language].Enqueue(app);
                    }
                }

                await Task.WhenAll(allDeleteTasks);

            }
        }

        async Task CreateNewAppsIfNeededFor(Language language)
        {
            var neededApps = _config.FreeAppQueueSize - _allFreeApps[language].Count;

            var newAppTasks = new List<Task<BaseApp>>();

            for (int i = 0; i < neededApps; i++)
            {
                newAppTasks.Add(language == Language.Ruby
                    ? LinuxApp.CreateApp(_client, _config, Logger, "Linuxfunclite-" + Guid.NewGuid())
                    : WindowsApp.CreateApp(_client, _config, Logger, "funclite-" + Guid.NewGuid()));
            }

            foreach (var app in await Task.WhenAll(newAppTasks))
            {
                _allFreeApps[language].Enqueue(app);
            }
        }


        async Task CreateNewAppsIfNeeded()
        {
            try
            {
                await CreateNewAppsIfNeededFor(Language.Node);
                await CreateNewAppsIfNeededFor(Language.Ruby);
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to create new apps: " + e.ToString());
            }
        }

        async Task BackgrounMaintenance()
        {
            try
            {
                var tasksmarkInUseTasks = new List<Task>();

                while (_allAppsToMarkAsInUse[Language.Node].Count > 0)
                {
                    tasksmarkInUseTasks.Add(_allAppsToMarkAsInUse[Language.Node].Dequeue().MarkAsUsed());
                }


                while (_allAppsToMarkAsInUse[Language.Ruby].Count > 0)
                {
                    tasksmarkInUseTasks.Add(_allAppsToMarkAsInUse[Language.Ruby].Dequeue().MarkAsUsed());
                }

                await Task.WhenAll(tasksmarkInUseTasks);

                // Create new apps if needed
                await CreateNewAppsIfNeeded();

                // Warm up all the apps in the free queue
                await WarmUpFreeApps();
            }
            catch (Exception)
            {
                // Ignore background task exceptions
            }
        }

        async Task WarmUpFreeApps()
        {
            // Keep all the free apps warm
            var appRefreshTasks = _allFreeApps[Language.Node].Select(app => ((WindowsApp) app).SendWarmUpRequests()).ToList();
            appRefreshTasks.AddRange(_allFreeApps[Language.Ruby].Select(app => ((LinuxApp) app).SendWarmUpRequests()));

            await Task.WhenAll(appRefreshTasks);
        }
    }
}
