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
        HttpClient _client;
        readonly MyConfig _config;

        public AppManager(IOptions<MyConfig> config)
        {
            _config = config.Value;

            string token = AuthenticationHelpers.AcquireTokenBySPN(
                _config.TenantId, _config.ClientId, _config.ClientSecret).Result;

            _client = new HttpClient(new LoggingHandler(new HttpClientHandler()));
            _client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            _client.BaseAddress = new Uri("https://management.azure.com/");

            TestFunctionAppOperations().Wait();
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

        async Task GetAllApps()
        {
            using (var response = await _client.GetAsync(
    $"/subscriptions/{_config.Subscription}/resourceGroups/{_config.ResourceGroup}/providers/Microsoft.Web/sites?api-version=2016-03-01"))
            {
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsAsync<dynamic>();
                foreach (var app in json.value)
                {
                    Console.WriteLine(app.name);
                    foreach (var hostname in app.properties.enabledHostNames)
                    {
                        Console.WriteLine("  " + hostname);
                    }
                }
            }

        }

        async Task CreateApp(string appName)
        {
            using (var response = await _client.PutAsJsonAsync(
                $"/subscriptions/{_config.Subscription}/resourceGroups/{_config.ResourceGroup}/providers/Microsoft.Web/sites/{appName}?api-version=2016-03-01",
                new
                {
                    location = _config.Region,
                    kind = "functionapp",
                    properties = new
                    {
                        siteConfig = new
                        {
                            appSettings = new object[]
                            {
                                new
                                {
                                    name = "FOO",
                                    value = "BAR"
                                }
                            }
                        }
                    }
                }))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        async Task TestFunctionAppOperations()
        {
            await CreateResourceGroup(_config.ResourceGroup);

            await GetAllApps();

            await CreateApp("funclite-" + Guid.NewGuid().ToString());
        }
    }
}
