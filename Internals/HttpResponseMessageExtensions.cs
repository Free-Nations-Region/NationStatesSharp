using Serilog;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace NationStatesSharp
{
    internal static class HttpResponseMessageExtensions
    {
        internal static async Task<XDocument> ReadXmlAsync(this HttpResponseMessage httpResponse, ILogger logger, CancellationToken cancellationToken)
        {
            if (httpResponse is null)
            {
                throw new ArgumentNullException(nameof(httpResponse));
            }

            if (httpResponse.IsSuccessStatusCode)
            {
                using (Stream stream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
                {
                    try
                    {
                        if (logger.IsEnabled(Serilog.Events.LogEventLevel.Verbose))
                        {
                            logger.Verbose(await httpResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
                        }
                        return await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken).ConfigureAwait(false);
                    }
                    catch (XmlException ex)
                    {
                        throw new ApplicationException($"A error while loading xml occured.", ex);
                    }
                }
            }
            else
            {
                return null;
            }
        }

        internal static async Task<Stream> ReadStreamAsync(this HttpResponseMessage httpResponse)
        {
            if (httpResponse is null)
            {
                throw new ArgumentNullException(nameof(httpResponse));
            }

            return httpResponse.IsSuccessStatusCode ? await httpResponse.Content.ReadAsStreamAsync().ConfigureAwait(false) : null;
        }

        internal static async Task<bool?> ReadBooleanAsync(this HttpResponseMessage httpResponse)
        {
            if (httpResponse is null)
            {
                throw new ArgumentNullException(nameof(httpResponse));
            }

            if (httpResponse.IsSuccessStatusCode)
            {
                var content = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                return bool.TryParse(content, out bool result) ? result : (bool?) null;
            }
            else
            {
                return null;
            }
        }
    }
}