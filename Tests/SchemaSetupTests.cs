using System;
using GraphQL.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class SchemaSetupTests
    {
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void IncompleteSchemaCantBeQueried()
        {
            var schema = new GraphQLSchema<object>(() => null);
            new GraphQL<object>(schema).ExecuteQuery("query users { id }");
        }
    }
}
