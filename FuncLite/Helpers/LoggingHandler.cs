using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FuncLite
{
    // LoggingHandler derived from http://stackoverflow.com/questions/12300458/web-api-audit-logging
    public class LoggingHandler : DelegatingHandler
    {
        readonly ILogger _logger;

        public LoggingHandler(HttpMessageHandler innerHandler, ILogger logger)
            : base(innerHandler)
        {
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            DateTime start = DateTime.Now;
            _logger.LogInformation($"Sending {request.Method} {request.RequestUri}");
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            var statusCode = response.StatusCode;
            TimeSpan timeTaken = DateTime.Now - start;
            _logger.LogInformation($"Done {request.Method} {request.RequestUri} --> {statusCode} in {timeTaken.TotalMilliseconds:0}ms");
            if (response.Content != null)
            {
                string responseContent = await response.Content.ReadAsStringAsync();

                if (((int)statusCode) >= 400)
                {
                    _logger.LogError(responseContent);
                }
            }

            return response;
        }
    }
}
