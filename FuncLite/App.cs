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

                var json = await response.Content.ReadAsAsync<dynamic>();
                return new App(client, json.properties);
            }
        }

        public App(HttpClient client, dynamic siteProps)
        {
            _client = client;
            _siteProps = siteProps;
        }
    }
}
