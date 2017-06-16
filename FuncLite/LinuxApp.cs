using System.Net.Http;
using System.Threading.Tasks;
using FuncLite;
using Microsoft.Extensions.Logging;

namespace FuncLite
{
    public class LinuxApp : BaseApp
    {
        public LinuxApp(HttpClient client, MyConfig config, ILogger<AppManager> logger, dynamic siteProps) : base(client, config, logger, new SitePropsWrapper(siteProps), Language.Ruby)
        {
        }

        public static async Task<BaseApp> CreateApp(HttpClient client, MyConfig config, ILogger<AppManager> logger, string appName)
        {
            using (var response = await client.PutAsJsonAsync(
                $"/subscriptions/{config.Subscription}/resourceGroups/{config.ResourceGroup}/providers/Microsoft.Web/sites/{appName}?api-version=2016-03-01",
                new
                {
                    location = config.Region,
                    properties = new
                    {
                        siteConfig = new
                        {
                            appSettings = new[]
                            {
                                new
                                {
                                    name = "DOCKER_CUSTOM_IMAGE_NAME",
                                    value = config.DockerImageName
                                },
                                new
                                {
                                    name = "DOCKER_REGISTRY_SERVER_URL",
                                    value = config.DockerServerURL
                                },
                                new
                                {
                                    name = "DOCKER_REGISTRY_SERVER_USERNAME",
                                    value = config.DockerRegistryUserName
                                },
                                new
                                {
                                    name = "DOCKER_REGISTRY_SERVER_PASSWORD",
                                    value = config.DockerRegistryPassword
                                }

                            },
                            appCommandLine = "",
                            linuxFxVersion = $"DOCKER|{config.DockerImageName}"
                        },
                        serverFarmId = config.ServerFarmId,
                    }
                }))
            {
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsAsync<dynamic>();
                var app = new LinuxApp(client, config, logger, json.properties);

                await app.CompleteCreation();

                return app;
            }
        }

        public async Task SendWarmUpRequests()
        {
            using (var response = await Client.GetAsync($"{SiteUrl}"))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        protected override Task UploadLanguageHost()
        {
            return Task.CompletedTask;
        }

        public async Task UploadUserCode(string zipPackagePath)
        {
            await EnsureScmHttpClient();
            using (var response = await ScmClient.PutZipFile($"{ScmBaseUrl}/api/zip/site/wwwroot/userFunc/", zipPackagePath))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        protected internal override async Task<dynamic> SendRequest(object payload)
        {
            using (var response = await Client.PostAsJsonAsync($"{SiteUrl}", payload))
            {
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsAsync<dynamic>();
            }
        }

    }
}