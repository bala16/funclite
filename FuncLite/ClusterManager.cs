using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace FuncLite
{
    public class ClusterManager
    {
        private readonly MyConfig _config;
        private readonly string _endPoint;
        private readonly int _portNumber;
        readonly HttpClient _client;

        private static readonly string FunctionEndpointTemplate = "{0}/api/{1}";
        private static readonly string ClusterEndpointTemplate = "{0}:{1}";

        public ClusterManager(IOptions<MyConfig> config, ILogger<AppManager> logger)
        {
            _config = config.Value;
            _endPoint = _config.ClusterEndPoint;
            _portNumber = int.Parse(this._config.ClusterPort);

            _client = new HttpClient(new LoggingHandler(new HttpClientHandler(), logger));
            _client.BaseAddress = new Uri(string.Format(ClusterEndpointTemplate, _endPoint, _portNumber));
        }

        public async Task CreateApplication(ComposeApplication composeApplication)
        {
            using (var response = await _client.PutAsJsonAsync(
                $"/ComposeDeployments/$/Create?api-version=4.0-preview&timeout=60",
                new
                {
                    ApplicationName = composeApplication.ApplicationName,
                    ComposeFileContent = composeApplication.ComposeFileContent,
                }))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task<dynamic> RunFunction(string functionName, string name)
        {
            var functionEndpoint = string.Format(FunctionEndpointTemplate, _config.ClusterEndPoint, functionName);
            using (var functionHttpClient = new HttpClient())
            {
                using (var response = await functionHttpClient.GetAsync($"{functionEndpoint}?name={name}"))
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync();
                }
            }
        }

        public async Task DeleteApplication(string applicationName)
        {
            using (var response = await _client.PostAsJsonAsync<string>($"/ComposeDeployments/{applicationName}/$/Delete?api-version=4.0-preview&timeout=60", null))
            {
                response.EnsureSuccessStatusCode();
            }
        }
    }
}
