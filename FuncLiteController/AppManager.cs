using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FuncLiteController
{
    public class AppManager
    {
        HttpClient _client;
        string _subscription;
        const string _resourceGroup = "MyResourceGroup";
        const string _location = "South Central US";

        public AppManager(IConfiguration config)
        {
            _subscription = config.GetValue<string>("AzureSubscription");

            string tenantId = config.GetValue<string>("AzureTenantId");
            string clientId = config.GetValue<string>("AzureClientId");
            string clientSecret = config.GetValue<string>("AzureClientSecret");

            string token = AuthenticationHelpers.AcquireTokenBySPN(tenantId, clientId, clientSecret).Result;

            _client = new HttpClient(new LoggingHandler(new HttpClientHandler()));
            _client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            _client.BaseAddress = new Uri("https://management.azure.com/");

            TestFunctionAppOperations().Wait();
        }

        async Task TestFunctionAppOperations()
        {
            // Create the Web App

            using (var response = await _client.PutAsJsonAsync(
                $"/subscriptions/{_subscription}/resourceGroups/{_resourceGroup}/providers/Microsoft.Web/sites/{"tmpfunc32586"}?api-version=2016-03-01",
                new
                {
                    location = _location,
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
    }
}
