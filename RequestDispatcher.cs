using NationStatesSharp.Interfaces;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NationStatesSharp
{
    public class RequestDispatcher : IRequestDispatcher
    {
        private bool _isRunning = false;
        private readonly CancellationTokenSource _tokenSource = new();
        private RequestWorker _worker;

        public RequestDispatcher(string userAgent, ILogger logger, bool doStart)
        {
            if(logger is null)
            {
                ConfigureLogging();
                logger = Log.Logger;
            }
            _worker = new RequestWorker(userAgent, logger);
            if (doStart)
            {
                Start();
            }
        }

        public RequestDispatcher(string userAgent, ILogger logger) : this(userAgent, logger, true)
        {
        }

        public RequestDispatcher(string userAgent): this(userAgent, null)
        {
        }

        public void Dispatch(Request request, uint priority = 1000)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));
            if (!_isRunning)
                throw new InvalidOperationException("Request cannot be dispatched when the dispatcher has not been started yet.");
            _worker.Enqueue(request, priority);
        }
        
        public void Dispatch(IEnumerable<Request> requests, uint priority = 1000)
        {
            if (requests is null)
                throw new ArgumentNullException(nameof(requests));
            if (!_isRunning)
                throw new InvalidOperationException("Requests cannot be dispatched when the dispatcher has not been started yet.");
            foreach (Request request in requests)
            {
                _worker.Enqueue(request, priority);
            }
        }

        private void RequestQueue_RestartRequired(object sender, EventArgs e)
        {
            if (sender is RequestWorker worker)
            {
                Task.Run(async () => await worker.RunAsync(_tokenSource.Token).ConfigureAwait(false));
            }
        }

        public void Shutdown()
        {
            _tokenSource.Cancel();
            _worker.RestartRequired -= RequestQueue_RestartRequired;
        }

        public void Start()
        {
            if (!_isRunning)
            {
                _isRunning = true;
                _worker.RestartRequired += RequestQueue_RestartRequired;
                Task.Run(async () => await _worker.RunAsync(_tokenSource.Token).ConfigureAwait(false));
            }
            else if(Log.Logger.IsEnabled(Serilog.Events.LogEventLevel.Debug))
            {
                Log.Logger.Debug("Attempted to start already running requestDispatcher.");
            }
        }

        private void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(theme: SystemConsoleTheme.Literate).CreateLogger();
        }

        public void AddToQueue(Request request, uint priority = 1000) => Dispatch(request, priority);
        public void AddToQueue(IEnumerable<Request> requests, uint priority = 1000) => Dispatch(requests, priority);
    }
}