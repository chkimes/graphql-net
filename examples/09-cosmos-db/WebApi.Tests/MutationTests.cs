using Newtonsoft.Json;
using System.Linq;
using WebApi.Models;
using WebApi.Services;
using WebApi.Tests.Models;
using Xunit;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Seeder = WebApi.Services.MyDocumentClientInitializer;

namespace WebApi.Tests
{
    public class MutationTests : BaseTests
    {
        //[Fact]
        //public async void Test1()
        //{
        //    // Act
        //    var resp = await Get(@"{ users { id } }");

        //    // Assert
        //    var actual = JObject.Parse(resp)["data"]["users"].Children().Select(j => j.ToObject<User>());

        //    actual.Should().BeEquivalentTo(Seeder.Users, options => options.Including(u => u.Id));
        //}
    }
}
