using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using FuncLite;
using Microsoft.Extensions.Logging;

namespace FuncLite
{
    public class WindowsApp : BaseApp
    {
        private readonly MyConfig _config;
        readonly ILogger _logger;
        HttpClient _scmClient;

        public WindowsApp(HttpClient client, MyConfig config, ILogger logger, dynamic siteProps) : base(client, config, logger, new SitePropsWrapper(siteProps), Language.Node)
        {
            _config = config;
            _logger = logger;
        }

        protected override async Task UploadLanguageHost()
        {
            await CreateKuduFolder(@"d:\home\SiteExtensions\FuncLite");

            await EnsureScmHttpClient();
            using (var response = await _scmClient.PutZipFile($"{ScmBaseUrl}/api/zip/SiteExtensions/FuncLite", $"{_config.DataFolder}/runtimes/node.zip"))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        public static async Task<BaseApp> CreateApp(HttpClient client, MyConfig config, ILogger<AppManager> logger, string appName)
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
                            appSettings = new[]
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
                var app = new WindowsApp(client, config, logger, json.properties);

                await app.CompleteCreation();

                return app;
            }
        }

        protected override async Task RestartScmSite()
        {
            // Just kill the scm site to restart it (faster than full site restart)
            using (var response = await Client.DeleteAsync($"{ScmBaseUrl}/api/processes/0"))
            {
                // Ignore errors as suiciding the scm w3wp can cause the delete request to fail (even though it still kills it)
                //response.EnsureSuccessStatusCode();
            }
        }

        async Task CreateKuduFolder(string folder)
        {
            await RunKuduCommand($"mkdir {folder}");
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

        public async Task UploadUserCode(string zipPackagePath)
        {
            await EnsureScmHttpClient();
            using (var response = await _scmClient.PutZipFile($"{ScmBaseUrl}/api/zip/LocalSiteRoot/funclite", zipPackagePath))
            {
                response.EnsureSuccessStatusCode();
            }
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

        public new async Task Delete()
        {
            using (var response = await Client.DeleteAsync(
                $"/subscriptions/{_config.Subscription}/resourceGroups/{_config.ResourceGroup}/providers/Microsoft.Web/sites/{AppName}?api-version=2016-03-01"))
            {
                response.EnsureSuccessStatusCode();
            }

            if (_scmClient != null)
            {
                _scmClient.Dispose();
                _scmClient = null;
            }
        }


        async Task EnsureScmHttpClient()
        {
            if (_scmClient == null)
            {
                using (var response = await Client.PostAsync(
                    $"/subscriptions/{_config.Subscription}/resourceGroups/{_config.ResourceGroup}/providers/Microsoft.Web/sites/{AppName}/config/publishingcredentials/list?api-version=2016-03-01",
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

        protected internal override async Task<dynamic> SendRequest(object payload)
        {
            await EnsureScmHttpClient();
            using (var response = await _scmClient.PostAsJsonAsync($"{ScmBaseUrl}/funclite", payload))
            {
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsAsync<dynamic>();
            }
        }
    }
}