namespace Fabric.Authorization.Client.IntegrationTests
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Xunit;

    public class AuthorizationClientTests
    {
        private AuthorizationClient subject;
        private HttpClient client;
        
        /// <summary>
        /// This is the test initializer.
        /// </summary>
        public AuthorizationClientTests()
        {
            client = new HttpClient();
            subject = new AuthorizationClient(client);
        }

        [Fact]
        public async Task Test1()
        {
            // Arrange
            // Todo: Change this to have a mocked httpclient
            client = new HttpClient();

            // Act
            var result = await subject.GetPermissionsForCurrentUser();

            // Assert
            Assert.NotNull(result);
        }
    }
}
