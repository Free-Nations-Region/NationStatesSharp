using NationStatesSharp.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NationStatesSharp
{
    internal class HttpDataService : IHttpDataService
    {
        private HttpMessageHandler _httpMessageHandler = null;
        private ILogger _logger;
        private IEnumerable<string> _userAgentParts;

        public HttpDataService(IEnumerable<string> userAgentParts, ILogger logger)
        {
            if (!userAgentParts.Any()) throw new InvalidOperationException("No Request can be send when no UserAgent has been provided.");
            if (logger is null) throw new ArgumentNullException(nameof(logger));
            _userAgentParts = userAgentParts;
            _logger = logger.ForContext<HttpDataService>();
        }

        public HttpDataService(string userAgent, ILogger logger) : this(new List<string>() { userAgent }, logger)
        {
        }

        public async Task<HttpResponseMessage> ExecuteRequestAsync(Request request, CancellationToken cancellationToken)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            using (HttpClient client = GetHttpClient())
            {
                foreach (var part in _userAgentParts)
                {
                    client.DefaultRequestHeaders.Add("User-Agent", part);
                }
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, request.Uri);
                _logger.Debug("[{traceId}] Executing {httpMethod}-Request to {requestUrl}", request.TraceId, requestMessage.Method, request.Uri);
                HttpResponseMessage response = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.Error("[{traceId}] Request finished with response: {StatusCode}: {ReasonPhrase}", request.TraceId, (int)response.StatusCode, response.ReasonPhrase);
                }
                else
                {
                    _logger.Debug("[{traceId}] Request finished with response: {StatusCode}: {ReasonPhrase}", request.TraceId, (int)response.StatusCode, response.ReasonPhrase);
                }
                return response;
            }
        }

        public HttpClient GetHttpClient() => _httpMessageHandler != null ? new HttpClient(_httpMessageHandler) : new HttpClient();

        public void SetHttpMessageHandler(HttpMessageHandler httpMessageHandler)
        {
            _httpMessageHandler = httpMessageHandler;
        }
    }
}