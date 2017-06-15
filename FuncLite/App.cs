using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace FuncLite
{
    public class App
    {
        readonly string _appName;
        readonly dynamic _siteProps;
        readonly HttpClient _client;
        HttpClient _scmClient;
        readonly MyConfig _config;
        readonly ILogger _logger;

        public static async Task<App> CreateApp(HttpClient client, MyConfig config, ILogger logger, string appName)
        {
            using (var response = await client.PutAsJsonAsync(
                $"/subscriptions/{config.Subscription}/resourceGroups/{config.ResourceGroup}/providers/Microsoft.Web/sites/{appName}?api-version=2016-03-01",
                new
                {
                    location = config.Region,
                    kind = "functionapp",
                    properties = new
                    {
                        siteConfig = new
                        {
                            appSettings = new []
                            {
                                new
                                {
                                    name = "FUNCFILE",
                                    value = "D:/local/funclite/index.js"
                                }
                            }
                        }
                    }
                }))
            {
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsAsync<dynamic>();
                var app = new App(client, config, logger, json.properties);

                await app.CompleteCreation();

                return app;
            }
        }

        public App(HttpClient client, MyConfig config, ILogger logger, dynamic siteProps)
        {
            _client = client;
            _config = config;
            _logger = logger;
            _siteProps = siteProps;
            _appName = siteProps.name;
        }

        string ScmBaseUrl
        {
            get
            {
                return $"https://{_siteProps.enabledHostNames[1]}";
            }
        }

        public bool InUse { get; private set; }

        public string Name => _appName;

        async Task EnsureScmHttpClient()
        {
            if (_scmClient == null)
            {
                using (var response = await _client.PostAsync(
                    $"/subscriptions/{_config.Subscription}/resourceGroups/{_config.ResourceGroup}/providers/Microsoft.Web/sites/{_appName}/config/publishingcredentials/list?api-version=2016-03-01",
                    null
                ))
                {
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsAsync<dynamic>();

                    _scmClient = new HttpClient(new LoggingHandler(new HttpClientHandler(), _logger));
                    var byteArray = Encoding.ASCII.GetBytes($"{json.properties.publishingUserName}:{json.properties.publishingPassword}");
                    _scmClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                }
            }
        }

        async Task CompleteCreation()
        {
            // Upload the lightweight host
            await UploadLanguageHost();

            // Restart the app so the site extension takes effect in the scm site
            await Restart();
        }

        public async Task SendWarmUpRequests()
        {
            // As a warmup request, create the folder where the user files will land, to make sure it's there is the site restarts
            await CreateKuduFolder(@"d:\local\funclite");

            await EnsureScmHttpClient();
            using (var response = await _scmClient.GetAsync($"{ScmBaseUrl}/funclite"))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task<dynamic> SendRequest(object payload)
        {
            await EnsureScmHttpClient();
            using (var response = await _scmClient.PostAsJsonAsync($"{ScmBaseUrl}/funclite", payload))
            {
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsAsync<dynamic>();
            }
        }

        async Task RunKuduCommand(string command)
        {
            await EnsureScmHttpClient();

            using (var response = await _scmClient.PostAsJsonAsync(
                $"{ScmBaseUrl}/api/command",
                new
                {
                    command = command
                }))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task Delete()
        {
            using (var response = await _client.DeleteAsync(
                $"/subscriptions/{_config.Subscription}/resourceGroups/{_config.ResourceGroup}/providers/Microsoft.Web/sites/{_appName}?api-version=2016-03-01"))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task MarkAsUsed()
        {
            // Mark it as used using the metadata collection, since that doesn't restart the site
            using (var response = await _client.PutAsJsonAsync(
                $"/subscriptions/{_config.Subscription}/resourceGroups/{_config.ResourceGroup}/providers/Microsoft.Web/sites/{_appName}/config/metadata?api-version=2016-03-01",
                new
                {
                    properties = new
                    {
                        IN_USE = 1
                    }
                }))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task<App> GetInUseState()
        {
            using (var response = await _client.PostAsync(
                $"/subscriptions/{_config.Subscription}/resourceGroups/{_config.ResourceGroup}/providers/Microsoft.Web/sites/{_appName}/config/metadata/list?api-version=2016-03-01",
                null
            ))
            {
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsAsync<dynamic>();
                InUse = (json.properties.IN_USE == "1");
                return this;
            }
        }

        async Task CreateKuduFolder(string folder)
        {
            await RunKuduCommand($"mkdir {folder}");
        }

        async Task UploadLanguageHost()
        {
            await CreateKuduFolder(@"d:\home\SiteExtensions\FuncLite");

            await EnsureScmHttpClient();
            using (var response = await _scmClient.PutZipFile($"{ScmBaseUrl}/api/zip/SiteExtensions/FuncLite", $"{_config.DataFolder}/runtimes/node.zip"))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task UploadUserCode(string zipPackagePath)
        {
            await EnsureScmHttpClient();
            using (var response = await _scmClient.PutZipFile($"{ScmBaseUrl}/api/zip/LocalSiteRoot/funclite", zipPackagePath))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        async Task Restart()
        {
            // Just kill the scm site to restart it (faster than full site restart)
            await EnsureScmHttpClient();
            using (var response = await _scmClient.DeleteAsync($"{ScmBaseUrl}/api/processes/0"))
            {
                // Ignore errors as suiciding the scm w3wp can cause the delete request to fail (even though it still kills it)
                //response.EnsureSuccessStatusCode();
            }
        }
    }
}
