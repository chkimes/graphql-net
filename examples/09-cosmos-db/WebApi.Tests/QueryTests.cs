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
    public class QueryTests : BaseTests
    {
        [Fact]
        public async void Users_With_IdAndProfile()
        {
            // Act
            var actual = await GetUsers(@"{ users { id, profile } }");

            // Assert
            actual.Should().BeEquivalentTo(Seeder.Users, options => options
                .Including(u => u.Id)
                .Including(u => u.Profile)
            );
        }

        [Fact]
        public async void Users_With_Id()
        {
            // Act
            var actual = await GetUsers(@"{ users { id } }");

            // Assert
            actual.Should().BeEquivalentTo(Seeder.Users, options => options.Including(u => u.Id));
        }

        [Fact]
        public async void Users_With_Profile()
        {
            // Act
            var users = await GetUsers(@"{ users { profile } }");

            // Assert
            users.Should().BeEquivalentTo(Seeder.Users, options => options.Including(u => u.Profile));
        }
    }
}
