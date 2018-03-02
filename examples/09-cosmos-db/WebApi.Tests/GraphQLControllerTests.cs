using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace WebApi.Tests
{
    public class GraphQLControllerTests
    {
        private readonly TestServer _server;
        private readonly HttpClient _client;

        public GraphQLControllerTests()
        {
            // Arrange

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.GetFullPath(@"../../../../WebApi/"))
                .AddJsonFile("appsettings.Development.json", optional: false)
                .Build();

            _server = new TestServer(new WebHostBuilder()
                .UseStartup<Startup>()
                .UseConfiguration(configuration)
            );

            _client = _server.CreateClient();
        }

        private async Task<string> Get(string query)
        {
            var request = "/graphql";
            if (!string.IsNullOrEmpty(query))
            {
                request += "?query=" + query;
            }
            var response = await _client.GetAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }


        [Fact]
        public async void Test1()
        {
            // Act
            var resp = await Get(@"{ users { id, profile } }");

            // Assert
            Assert.Equal("expected", resp);
        }
    }
}
