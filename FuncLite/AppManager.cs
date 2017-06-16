using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FuncLite
{

    public class AppManager
    {
        const string InUseFilePath = "AppsInUse.json";

        readonly HttpClient _client;
        readonly MyConfig _config;
        static readonly object _inUseFileLock = new object();

        private readonly Dictionary<Language, Queue<BaseApp>> _allFreeApps = new Dictionary<Language, Queue<BaseApp>>();

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

            Init().Wait();

            CreateNewAppsIfNeeded().Wait();

            Logger.LogInformation("Setting up background timer");
            new Timer(_ => BackgroundMaintenance().Wait(), null, 0, 60000);
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

            MarkAppAsInUse(readyApp);

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

                var allApps = await response.Content.ReadAsAsync<dynamic>();

                // Delete any app we find that has been used before
                var allDeleteTasks = new List<Task>();

                JObject appsInUse = GetInUseAppNames();

                foreach (var appProps in allApps.value)
                {
                    BaseApp app;
                    string appName = appProps.properties.name;
                    if (appName.StartsWith("Linux"))
                    {
                        app = new LinuxApp(_client, _config, Logger, appProps.properties);
                    }
                    else
                    {
                        app = new WindowsApp(_client, _config, Logger, appProps.properties);
                    }

                    if (appsInUse[appName] != null)
                    {
                        allDeleteTasks.Add(app.Delete());
                    }
                    else
                    {
                        if (app is LinuxApp)
                        {
                            _allFreeApps[Language.Ruby].Enqueue(app);
                        }
                        else
                        {
                            _allFreeApps[Language.Node].Enqueue(app);
                        }
                    }
                }

                await Task.WhenAll(allDeleteTasks);

                if (File.Exists(InUseFilePath))
                {
                    File.Delete(InUseFilePath);
                }
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
                    : WindowsApp.CreateApp(_client, _config, Logger, "funclite-" + Environment.TickCount + "-" + Guid.NewGuid()));
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

        JObject GetInUseAppNames()
        {
            if (!File.Exists(InUseFilePath))
            {
                return new JObject();
            }

            return JObject.Parse(File.ReadAllText(InUseFilePath));
        }

        void MarkAppAsInUse(BaseApp app)
        {
            lock (_inUseFileLock)
            {
                JObject appsInUse = GetInUseAppNames();
                appsInUse[app.Name] = true;

                File.WriteAllText(InUseFilePath, JsonConvert.SerializeObject(appsInUse));
            }
        }

        async Task BackgroundMaintenance()
        {
            Logger.LogInformation("BackgroundMaintenance starts");

            try
            {
                // Create new apps if needed
                await CreateNewAppsIfNeeded();

                // Warm up all the apps in the free queue
                await WarmUpFreeApps();
            }
            catch (Exception)
            {
                // Ignore background task exceptions
            }

            Logger.LogInformation("BackgroundMaintenance complete");
        }

        async Task WarmUpFreeApps()
        {
            Logger.LogInformation("WarmUpFreeApps");

            // Keep all the free apps warm
            var appRefreshTasks = _allFreeApps[Language.Node].Select(app => ((WindowsApp) app).SendWarmUpRequests()).ToList();
            appRefreshTasks.AddRange(_allFreeApps[Language.Ruby].Select(app => ((LinuxApp) app).SendWarmUpRequests()));

            await Task.WhenAll(appRefreshTasks);
        }
    }
}
