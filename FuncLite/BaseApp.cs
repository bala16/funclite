using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace FuncLite
{
    public abstract class BaseApp
    {
        protected readonly string AppName;
        private readonly dynamic _siteProps;
        protected readonly HttpClient Client;
        private readonly MyConfig _config;
        public Language Language { get; }

        protected BaseApp(HttpClient client, MyConfig config, ILogger logger, SitePropsWrapper sitePropsWrapper, Language language)
        {
            Client = client;
            _config = config;
            _siteProps = sitePropsWrapper.SiteProps;
            AppName = _siteProps.name;
            Language = language;
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
            await Task.Delay(1000);

            // Upload the lightweight host
            await UploadLanguageHost();

            // Restart the app so the site extension takes effect in the scm site
            await RestartScmSite();
        }

    }
}
