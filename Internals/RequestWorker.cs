using NationStatesSharp.Enums;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NationStatesSharp
{
    internal class RequestWorker
    {
        public const long API_REQUEST_INTERVAL = 6000000; //0,6 s
        private readonly HttpDataService _dataService;
        private readonly ILogger _logger;

        private readonly RequestPriorityQueue _requestQueue = new();
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private RequestWorker()
        {
            _requestQueue.Jammed += RequestQueue_Jammed;
        }

        public RequestWorker(IEnumerable<string> userAgentParts, ILogger logger) : this()
        {
            if (!userAgentParts.Any())
                throw new InvalidOperationException("No Request can be send when no UserAgent has been provided.");
            _dataService = new HttpDataService(userAgentParts, logger);
            _logger = logger.ForContext<RequestWorker>();
        }

        public RequestWorker(string userAgent, ILogger logger) : this(new List<string>() { userAgent }, logger)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
                throw new InvalidOperationException("No Request can be send when contact info hasn't been provided.");
        }

        private void RequestQueue_Jammed(object sender, EventArgs e)
        {
            _logger.Warning("RequestQueue jammed. No waiting consumers found. Consumers possibly died. Trying to restart worker.");
            RestartRequired?.Invoke(this, e);
        }

        public event EventHandler RestartRequired;

        public void Enqueue(Request request, uint priority = 1000)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));
            _requestQueue.Enqueue(request, priority);
            if (_requestQueue.Count > 0)
            {
                _logger.Debug("Request [{traceId}] has been queued. Queue size: {size}", request.TraceId, _requestQueue.Count);
            }
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            async void ContinueAsync(Task<HttpResponseMessage> prev)
            {
                await Task.Delay(TimeSpan.FromTicks(API_REQUEST_INTERVAL)).ConfigureAwait(false);
                if (_semaphore.CurrentCount == 0)
                {
                    _semaphore.Release();
                }
            }
            bool isProcessing = true;
            while (isProcessing)
            {
                await _requestQueue.WaitForNextItemAsync(cancellationToken).ConfigureAwait(false);
                var ticks = DateTime.UtcNow.Ticks;
                var request = _requestQueue.Dequeue();
                if (request.Status != RequestStatus.Pending)
                {
                    continue;
                }
                _logger.Debug("Request [{traceId}] has been dequeued. Queue size: {size}", request.TraceId, _requestQueue.Count);
                _logger.Verbose("[{traceId}]: Acquiring Semaphore", request.TraceId);
                await _semaphore.WaitAsync().ConfigureAwait(false);
                _logger.Verbose("[{traceId}]: Aquiring Semaphore took {duration}", request.TraceId, TimeSpan.FromTicks(DateTime.UtcNow.Ticks).Subtract(TimeSpan.FromTicks(ticks)));
                try
                {
                    var task = _dataService.ExecuteRequestAsync(request, cancellationToken);
                    _ = task.ContinueWith(ContinueAsync, cancellationToken);
                    var httpResponse = await task.ConfigureAwait(false);
                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        request.Fail(new HttpRequestFailedException($"{(int) httpResponse.StatusCode} - {httpResponse.ReasonPhrase}"));
                    }
                    if (request.ResponseFormat == ResponseFormat.Xml)
                    {
                        request.Complete(await httpResponse.ReadXmlAsync(_logger, cancellationToken).ConfigureAwait(false));
                    }
                    else if (request.ResponseFormat == ResponseFormat.Boolean)
                    {
                        request.Complete(await httpResponse.ReadBooleanAsync().ConfigureAwait(false));
                    }
                    else if (request.ResponseFormat == ResponseFormat.Stream)
                    {
                        request.Complete(await httpResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false));
                    }
                    else
                    {
                        throw new NotImplementedException($"Unknown ResponseFormat: {request.ResponseFormat}");
                    }
                }
                catch (Exception ex) when (ex is TaskCanceledException)
                {
                    isProcessing = false;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "A error occurred.");
                    request.Fail(ex);
                }
            }
        }
    }
}