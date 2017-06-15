using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Fabric.Authorization.MetadataFetcher
{
    public class AzureADGroupFetcher : IGroupFetcher
    {
        public IEnumerable<string> FetchAllGroups(Dictionary<string, string> props)
        {
            var tenantId = props["AzureAD.TenantID"];
            var authToken = props["AzureAD.Authorization"];
            var apiVersion = "?api-version=1.6";
            props.TryGetValue("AzureAD.Filter", out var filter);


            var httpClient = new HttpClient()
            {
                BaseAddress = new Uri(props["AzureAD.BaseURL"])
            };

            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

            var httpRequest = new HttpRequestMessage(new HttpMethod("GET"), $"/{tenantId}/groups{apiVersion}{filter}");
            HttpResponseMessage response = httpClient.SendAsync(httpRequest).Result;
            if (response.IsSuccessStatusCode)
            {
                // Parse the response body. Blocking!
                var content = response.Content.ReadAsStringAsync().Result;//response.Content.ReadAsAsync<IEnumerable<string>>().Result;
                Console.WriteLine($"Content: {content}");
            }
            else
            {
                Console.WriteLine(response.StatusCode);
            }

            return Enumerable.Empty<string>();
        }
    }
}
