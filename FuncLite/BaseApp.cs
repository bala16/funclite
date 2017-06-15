using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FuncLite
{
    public abstract class BaseApp
    {
        protected readonly string AppName;
        private readonly dynamic _siteProps;
        protected readonly HttpClient Client;
        private readonly MyConfig _config;
        protected HttpClient ScmClient;
        protected readonly ILogger _logger;

        public Language Language { get; }

        protected BaseApp(HttpClient client, MyConfig config, ILogger logger, SitePropsWrapper sitePropsWrapper, Language language)
        {
            Client = client;
            _config = config;
            _siteProps = sitePropsWrapper.SiteProps;
            AppName = _siteProps.name;
            Language = language;
            _logger = logger;
        }

        protected async Task EnsureScmHttpClient()
        {
            if (ScmClient == null)
            {
                using (var response = await Client.PostAsync(
                    $"/subscriptions/{_config.Subscription}/resourceGroups/{_config.ResourceGroup}/providers/Microsoft.Web/sites/{AppName}/config/publishingcredentials/list?api-version=2016-03-01",
                    null
                ))
                {
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsAsync<dynamic>();

                    ScmClient = new HttpClient(new LoggingHandler(new HttpClientHandler(), _logger));
                    var byteArray = Encoding.ASCII.GetBytes($"{json.properties.publishingUserName}:{json.properties.publishingPassword}");
                    ScmClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                }
            }
        }

        public bool InUse { get; private set; }

        public string Name => AppName;

        protected string SiteUrl
        {
            get
            {
                return $"https://{_siteProps.enabledHostNames[0]}";
            }
        }
        protected string ScmBaseUrl
        {
            get
            {
                return $"https://{_siteProps.enabledHostNames[1]}";
            }
        }

        public async Task Delete()
        {
            using (var response = await Client.DeleteAsync(
                $"/subscriptions/{_config.Subscription}/resourceGroups/{_config.ResourceGroup}/providers/Microsoft.Web/sites/{AppName}?api-version=2016-03-01"))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        protected abstract Task UploadLanguageHost();
        protected internal abstract Task<dynamic> SendRequest(object payload);

        protected virtual Task RestartScmSite()
        {
            // Do nothing here as Linux doesn't need that
            return Task.CompletedTask;
        }

        protected async Task CompleteCreation()
        {
            // Wait one second before hitting it to make sure the DNS propagates
            await Task.Delay(5000);

            // Upload the lightweight host
            await UploadLanguageHost();

            // Restart the app so the site extension takes effect in the scm site
            await RestartScmSite();
        }

    }
}
