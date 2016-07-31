using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class IntrospectionTests
    {
        [Test]
        public void TypeDirectFields()
        {
            var gql = MemContext.CreateDefaultContext();
            var results = gql.ExecuteQuery("{ __type(name: \"User\") { name, description, kind } }");
            Test.DeepEquals(results, "{ __type: { name: 'User', description: '', kind: 'OBJECT' } }");
        }

        [Test]
        public void TypeWithChildFields()
        {
            var gql = MemContext.CreateDefaultContext();
            var results =  gql.ExecuteQuery("{ __type(name: \"User\") { fields { name } } }");
            Test.DeepEquals(results, 
                @"{
                      __type: {
                          fields: [
                              { name: 'id' },
                              { name: 'name' },
                              { name: 'account' },
                              { name: 'nullRef' },
                              { name: 'total' },
                              { name: 'accountPaid' },
                              { name: 'abc' },
                              { name: 'sub' },
                              { name: '__typename' }
                          ]
                      }
                  }");
        }

        [Test]
        [Ignore("This test depends on issue #30 (null checks for in-memory queries)")]
        public void ChildFieldType()
        {
            var gql = MemContext.CreateDefaultContext();
            var results =  gql.ExecuteQuery("{ __type(name: \"User\") { fields { name, type { name, kind, ofType { name, kind } } } } }");
            Test.DeepEquals(results,
                @"{
                      __type: {
                          fields: [
                              { name: 'id', type: { name: null, kind: 'NON_NULL', ofType: { name: 'Int', kind: 'SCALAR' } } },
                              { name: 'name', type: { name: 'String', kind: 'SCALAR' } },
                              { name: 'account', type: { name: 'Account', kind: 'OBJECT' } },
                              { name: 'total', type: { name: null, kind: 'NON_NULL', ofType: { name: 'Int', kind: 'SCALAR' } } },
                              { name: 'accountPaid', { name: null, kind: 'NON_NULL', ofType: { name: 'Boolean', kind: 'SCALAR' } } },
                              { name: 'abc', type: { name: 'String', kind: 'SCALAR' } },
                              { name: 'sub', type: { name: 'Sub', kind: 'OBJECT' } },
                              { name: '__typename', type: { name: 'String', kind: 'SCALAR' } }
                          ]
                      }
                  }");
        }

        [Test]
        public void SchemaTypes()
        {
            // TODO: Use Test.DeepEquals once we get all the primitive type noise sorted out

            var gql = MemContext.CreateDefaultContext();
            var schema = (IDictionary<string, object>) gql.ExecuteQuery("{ __schema { types { name, kind, interfaces { name } } } }")["__schema"];
            var types = (List<IDictionary<string, object>>) schema["types"];

            var intType = types.First(t => (string) t["name"] == "Int");
            Assert.AreEqual(intType["name"], "Int");
            Assert.AreEqual(intType["kind"].ToString(), "SCALAR");
            Assert.IsNull(intType["interfaces"]);

            var userType = types.First(t => (string) t["name"] == "User");
            Assert.AreEqual(userType["name"], "User");
            Assert.AreEqual(userType["kind"].ToString(), "OBJECT");
            Assert.AreEqual(((List<IDictionary<string, object>>)userType["interfaces"]).Count, 0);
        }
    }
}
