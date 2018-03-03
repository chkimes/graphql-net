using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WebApi.Models;

namespace WebApi.Tests
{
    public class BaseTests
    {
        private readonly TestServer _server;
        private readonly HttpClient _client;

        public BaseTests()
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

        protected async Task<string> Get(string query)
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

        protected async Task<IEnumerable<User>> GetUsers(string query)
        {
            var resp = await Get(query);
            var users = JObject.Parse(resp)["data"]["users"].Children().Select(j => j.ToObject<User>());
            return users;
        }

        protected async Task<User> GetUser(string query)
        {
            var resp = await Get(query);
            var user = JObject.Parse(resp)["data"]["user"].ToObject<User>();
            return user;
        }
    }
}
