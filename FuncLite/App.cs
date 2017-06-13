using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FuncLite
{
    public class App
    {
        readonly dynamic _siteProps;
        readonly HttpClient _client;
        readonly MyConfig _config;

        public static async Task<App> CreateApp(HttpClient client, MyConfig config, string appName)
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
                var app = new App(client, config, json.properties);

                // Upload the lightweight host
                await app.UploadLanguageHost();

                // Restart the app so the site extension takes effect in the scm site
                await app.Restart();

                return app;
            }
        }

        public App(HttpClient client, MyConfig config, dynamic siteProps)
        {
            _client = client;
            _config = config;
            _siteProps = siteProps;
        }

        string ScmBaseUrl
        {
            get
            {
                return $"https://{_siteProps.enabledHostNames[1]}";
            }
        }

        public async Task SendWarmUpRequest()
        {
            using (var response = await _client.GetAsync(ScmBaseUrl))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task SendRequest(object payload)
        {
            using (var response = await _client.PutAsJsonAsync(ScmBaseUrl, payload))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        async Task UploadLanguageHost()
        {
            using (var response = await _client.PutZipFile($"{ScmBaseUrl}/api/zip", $"{_config.DataFolder}/runtimes/node.zip"))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        async Task Restart()
        {
            // Just kill the scm site to restart it (faster than full site restart)
            using (var response = await _client.DeleteAsync($"{ScmBaseUrl}/api/processes/0"))
            {
                // Ignore errors as suiciding the scm w3wp can cause the delete request to fail (even though it still kills it)
                //response.EnsureSuccessStatusCode();
            }
        }
    }
}
