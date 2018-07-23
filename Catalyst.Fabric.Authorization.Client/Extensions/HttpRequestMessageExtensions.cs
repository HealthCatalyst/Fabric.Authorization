using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace Catalyst.Fabric.Authorization.Client.Extensions
{
    internal static class HttpRequestMessageExtensions
    {
        public static HttpRequestMessage AddBearerToken(this HttpRequestMessage httpRequestMessage, string accessToken)
        {
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return httpRequestMessage;
        }

        public static HttpRequestMessage AddAcceptHeader(this HttpRequestMessage httpRequestMessage, string mediaType = ClientConstants.ApplicationJson)
        {
            httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
            return httpRequestMessage;
        }

        public static HttpRequestMessage AddContent<T>(this HttpRequestMessage httpRequestMessage, T model, string mediaType = ClientConstants.ApplicationJson)
        {
            httpRequestMessage.Content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, mediaType);
            return httpRequestMessage;
        }
    }
}