using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NationStatesSharp
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static class XmlExtensions
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
        public static IEnumerable<string> GetAttributeValuesByAttributeName(this IEnumerable<XElement> elements, string attributeName)
        {
            foreach (var element in elements)
            {
                yield return element.GetAttributeValueByAttributeName(attributeName);
            }
        }

        public static Task WaitForAllResponsesAsync(this IEnumerable<Request> requests, CancellationToken cancellationToken) => Task.WhenAll(requests.Select(r => r.WaitForResponseAsync(cancellationToken)));

        public static Task WaitForAllResponsesAsync(this IEnumerable<Request> requests) => requests.WaitForAllResponsesAsync(CancellationToken.None);

        public static string GetAttributeValueByAttributeName(this XElement element, string attributeName) => element.Attributes().FirstOrDefault(e => e.Name == attributeName)?.Value;

        public static IEnumerable<XElement> FilterDescendantsByNameAndValue(this XDocument document, string nodeName, string value) => document?.Descendants(nodeName).Where(e => e.Value == value);

        public static string GetFirstValueByNodeName(this XDocument document, string nodeName) => document?.Descendants(nodeName).FirstOrDefault()?.Value;

        public static IEnumerable<XElement> GetParentsOfFilteredDescendants(this XDocument document, string nodeName, string value) => document?.FilterDescendantsByNameAndValue(nodeName, value).Select(e => e.Parent);
    }
}