using System;
using GraphQL.Net;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class SchemaSetupTests
    {
        [Test]
        public void IncompleteSchemaCantBeQueried()
        {
            var schema = new GraphQLSchema<object>(() => null);
            Assert.Throws<InvalidOperationException>(() => new GraphQL<object>(schema).ExecuteQuery("query users { id }"));
        }
    }
}
