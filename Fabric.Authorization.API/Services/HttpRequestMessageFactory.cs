using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Fabric.Platform.Http;
using IdentityModel.Client;

namespace Fabric.Authorization.API.Services
{
	public class HttpRequestMessageFactory : IHttpRequestMessageFactory
	{
		private readonly string _correlationToken;
		private readonly string _subject;
		private readonly TokenClient _tokenClient;

		public HttpRequestMessageFactory(string tokenUrl, string clientId, string secret, string correlationToken, string subject)
		{
			_correlationToken = correlationToken;
			_subject = subject;
			_tokenClient = new TokenClient(tokenUrl, clientId, secret);
		}
		public async Task<HttpRequestMessage> Create(HttpMethod httpMethod, Uri uri, string requestScope)
		{
			var response = await _tokenClient.RequestClientCredentialsAsync(requestScope).ConfigureAwait(false);
			return CreateWithAccessToken(httpMethod, uri, response.AccessToken);
		}

		public HttpRequestMessage CreateWithAccessToken(HttpMethod httpMethod, Uri uri, string accessToken)
		{
			var httpRequestMessage = new HttpRequestMessage(httpMethod, uri);
			httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

			httpRequestMessage.Headers.Add(Platform.Shared.Constants.FabricHeaders.CorrelationTokenHeaderName, _correlationToken);
			if (!string.IsNullOrEmpty(_subject))
			{
				httpRequestMessage.Headers.Add(Platform.Shared.Constants.FabricHeaders.SubjectNameHeader, _subject);
			}
			return httpRequestMessage;
		}
	}
}
