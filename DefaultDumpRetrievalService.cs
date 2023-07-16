using NationStatesSharp.Enums;
using NationStatesSharp.Interfaces;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace NationStatesSharp
{
    ///<inheritdoc/>
    public class DefaultDumpRetrievalService : IDumpRetrievalService
    {
        private readonly IRequestDispatcher _dispatcher;
        private readonly ILogger _logger;
        /// <summary>
        /// Creates DefaultDumpRetrievalService
        /// </summary>
        /// <param name="requestDispatcher">The requestDispatcher to use</param>
        /// <param name="logger">The logger to use</param>
        public DefaultDumpRetrievalService(IRequestDispatcher requestDispatcher, ILogger logger)
        {
            _dispatcher = requestDispatcher;
            _logger = logger.ForContext<DefaultDumpRetrievalService>();
        }
        /// <summary>
        /// Creates DefaultDumpRetrievalService
        /// </summary>
        /// <param name="requestDispatcher">The requestDispatcher to use</param>
        public DefaultDumpRetrievalService(IRequestDispatcher requestDispatcher) : this(requestDispatcher, Log.Logger)
        {
        }


        ///<inheritdoc/>
        ///<exception cref="HttpRequestFailedException">Thrown when the HTTP Request failed.</exception>
        public async Task<Stream> DownloadDumpAsync(DumpType type, CancellationToken cancellationToken) => await DownloadDumpAsync($"pages/{GetDumpFileNameByDumpType(type)}", cancellationToken).ConfigureAwait(false);

        ///<inheritdoc/>
        ///<exception cref="HttpRequestFailedException">Thrown when the HTTP Request failed.</exception>
        public async Task<Stream> DownloadDumpAsync(string url, CancellationToken cancellationToken)
        {
            var request = new Request(url, ResponseFormat.Stream);
            _dispatcher.Dispatch(request, 10000);
            await request.WaitForResponseAsync(cancellationToken).ConfigureAwait(false);
            return request.GetResponseAsStream();
        }

        ///<inheritdoc/>
        /// <exception cref="InvalidOperationException">Thrown when the DumpType is not supported.</exception>
        public string GetDumpFileNameByDumpType(DumpType type)
        {
            switch (type)
            {
                case DumpType.Nations:
                    return "nations.xml.gz";

                case DumpType.Regions:
                    return "regions.xml.gz";

                case DumpType.Cards1:
                    return "cardlist_S1.xml.gz";

                case DumpType.Cards2:
                    return "cardlist_S2.xml.gz";

                default:
                    throw new InvalidOperationException($"Unsupported DumpType {type}");
            }
        }

        ///<inheritdoc/>
        public bool IsLocalDumpAvailableAndUpToDate(DumpType type) => IsLocalDumpAvailableAndUpToDate(GetDumpFileNameByDumpType(type));

        ///<inheritdoc/>
        public bool IsLocalDumpAvailableAndUpToDate(string path)
        {
            if (File.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                var outdated = fileInfo.LastWriteTimeUtc.Date != DateTime.UtcNow.Date;
                // Update time is 22:30 PM PST -> 6:30 AM UTC
                if (DateTime.UtcNow.TimeOfDay < new TimeSpan(6, 30, 0) && outdated)
                {
                    outdated = false;
                }
                if (outdated)
                {
                    _logger.Debug("Local DumpData ({path}) found but outdated.", path);
                }
                else
                {
                    _logger.Debug("Local DumpData ({path}) found.", path);
                }
                return !outdated;
            }
            else
            {
                _logger.Debug("No Local DumpData ({path}) found", path);
                return false;
            }
        }
    }
}