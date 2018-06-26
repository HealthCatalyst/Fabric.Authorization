using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace Fabric.Authorization.Client.Extensions
{
    internal static class HttpRequestMessageExtensions
    {
        public static HttpRequestMessage AddBearerToken(this HttpRequestMessage httpRequestMessage, string accessToken)
        {
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return httpRequestMessage;
        }

        public static HttpRequestMessage AddAcceptHeader(this HttpRequestMessage httpRequestMessage, string contentType)
        {
            httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));
            return httpRequestMessage;
        }

        public static HttpRequestMessage AddContent<T>(this HttpRequestMessage httpRequestMessage, T model)
        {
            httpRequestMessage.Content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8,
                ClientConstants.ApplicationJson);

            return httpRequestMessage;
        }
    }
}