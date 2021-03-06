﻿using NationStatesSharp.Interfaces;
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

        public RequestDispatcher(string userAgent, ILogger logger)
        {
            _worker = new RequestWorker(userAgent, logger);
        }

        public RequestDispatcher(string userAgent)
        {
            ConfigureLogging();
            _worker = new RequestWorker(userAgent, Log.Logger);
        }

        public void Dispatch(Request request, int priority = 1000)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));
            if (!_isRunning)
                throw new InvalidOperationException("Request cannot be dispatched when the dispatcher has not been started yet.");
            _worker.Enqueue(request, priority);
        }

        public void Dispatch(IEnumerable<Request> requests, int priority = 1000)
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
            else
            {
                throw new InvalidOperationException("The dispatcher is already running.");
            }
        }

        private void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(theme: SystemConsoleTheme.Literate).CreateLogger();
        }
    }
}