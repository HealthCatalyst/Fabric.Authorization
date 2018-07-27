using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace Catalyst.Fabric.Authorization.Client.Extensions
{
    internal static class AuthorizationExtensions
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

        public static HttpClient FormatBaseUrl(this HttpClient client)
        {
            // so this will trim the forward slash whether its there or not.  it will 
            // also fix any additional forward slashes.  Once it has trimmed the end,
            // it will append the forward slash needed for making accurate calls.
            // see also: https://stackoverflow.com/questions/23438416/why-is-httpclient-baseaddress-not-working#23438417
            client.BaseAddress = new Uri($"{client.BaseAddress.OriginalString.TrimEnd('/')}/");
            return client;
        }
    }
}