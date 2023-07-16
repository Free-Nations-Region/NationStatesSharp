using NationStatesSharp.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace NationStatesSharp
{
    public class Request
    {
        private const string BaseUrl = "https://nationstates.net/";

        private Request(string traceId)
        {
            TraceId = string.IsNullOrWhiteSpace(traceId) ? GenerateTraceId() : traceId;
            Status = RequestStatus.Pending;
            _completionSource = new TaskCompletionSource();
        }

        public Request(string url, ResponseFormat responseFormat, string traceId = null) : this(traceId)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException($"\"{nameof(url)}\" cannot be null or whitespace.", nameof(url));
            }
            if (url.Contains("pages") && Uri.TryCreate(new Uri(BaseUrl), url, out Uri parsedUri))
            {
                Uri = parsedUri;
            }
            else if (Uri.TryCreate(new Uri(BaseUrl), "cgi-bin/api.cgi?" + url, out parsedUri))
            {
                Uri = parsedUri;
            }
            else
            {
                throw new UriFormatException($"{BaseUrl}/{url} could not be parsed into a valid uri.");
            }
            ResponseFormat = responseFormat;
        }

        public ResponseFormat ResponseFormat { get; private set; }
        public Uri Uri { get; private set; }
        private readonly TaskCompletionSource _completionSource;
        public string TraceId { get; private set; }
        public object Response { get; private set; }
        public RequestStatus Status { get; private set; }

        public void Complete(object response)
        {
            Response = response;
            Status = RequestStatus.Success;
            _completionSource?.TrySetResult();
        }

        public void Fail(Exception ex)
        {
            Status = RequestStatus.Failed;
            if (ex is null)
            {
                ex = new Exception("Error not specified.");
            }

            _completionSource?.TrySetException(ex);
        }

        public Task WaitForResponseAsync()
        {
            return WaitForResponseAsync(CancellationToken.None);
        }

        public Task WaitForResponseAsync(CancellationToken cancellationToken)
        {
            cancellationToken.Register(() =>
            {
                Status = RequestStatus.Canceled;
                _completionSource.TrySetCanceled(cancellationToken);
            });
            return _completionSource?.Task;
        }

        public XDocument GetResponseAsXml() => Response as XDocument;

        public bool? GetResponseAsBoolean() => Response as bool?;

        public Stream GetResponseAsStream() => Response as Stream;

        public async Task<XDocument> GetXmlResponseAsync(CancellationToken cancellationToken = default)
        {
            await WaitForResponseAsync(cancellationToken).ConfigureAwait(false);
            return GetResponseAsXml();
        }

        public async Task<bool?> GetBoolResponseAsync(CancellationToken cancellationToken = default)
        {
            await WaitForResponseAsync(cancellationToken).ConfigureAwait(false);
            return GetResponseAsBoolean();
        }

        public async Task<Stream> GetStreamResponseAsync(CancellationToken cancellationToken = default)
        {
            await WaitForResponseAsync(cancellationToken).ConfigureAwait(false);
            return GetResponseAsStream();
        }

        private string GenerateTraceId()
        {
            StringBuilder builder = new StringBuilder();
            Enumerable
               .Range(65, 26)
                .Select(e => ((char) e).ToString())
                .Concat(Enumerable.Range(97, 26).Select(e => ((char) e).ToString()))
                .Concat(Enumerable.Range(0, 10).Select(e => e.ToString()))
                .OrderBy(e => Guid.NewGuid())
                .Take(11)
                .ToList().ForEach(e => builder.Append(e));
            return builder.ToString();
        }
    }
}