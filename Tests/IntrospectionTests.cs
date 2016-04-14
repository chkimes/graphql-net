using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            var type = (IDictionary<string, object>)gql.ExecuteQuery("{ __type(name: \"User\") { name, description, kind } }")["__type"];
            Assert.AreEqual(type["name"], "User");
            Assert.AreEqual(type["description"], "");
            Assert.AreEqual(type["kind"], "OBJECT");
            Assert.AreEqual(type.Keys.Count, 3);
        }

        [Test]
        public void TypeWithChildFields()
        {
            var gql = MemContext.CreateDefaultContext();
            var type = (IDictionary<string, object>) gql.ExecuteQuery("{ __type(name: \"User\") { fields { name } } }")["__type"];
            Assert.AreEqual(type.Keys.Count, 1);
            var fields = (List<IDictionary<string, object>>) type["fields"];
            Assert.IsTrue(fields.Any(f => (string)f["name"] == "id"));
            Assert.IsTrue(fields.Any(f => (string)f["name"] == "name"));
            Assert.IsTrue(fields.Any(f => (string)f["name"] == "account"));
        }

        [Test]
        public void ChildFieldType()
        {
            var gql = MemContext.CreateDefaultContext();
            var type = (IDictionary<string, object>) gql.ExecuteQuery("{ __type(name: \"User\") { fields { name, type { name, kind } } } }")["__type"];
            Assert.AreEqual(type.Keys.Count, 1);
            var fields = (List<IDictionary<string, object>>) type["fields"];

            var idField = fields.First(f => (string) f["name"] == "id");
            var idType = (IDictionary<string, object>) idField["type"];
            Assert.AreEqual(idType["name"], "Int");
            Assert.AreEqual(idType["kind"], "SCALAR");

            var nameField = fields.First(f => (string) f["name"] == "name");
            var nameType = (IDictionary<string, object>) nameField["type"];
            Assert.AreEqual(nameType["name"], "String");
            Assert.AreEqual(nameType["kind"], "SCALAR");

            var accountField = fields.First(f => (string) f["name"] == "account");
            var accountType = (IDictionary<string, object>) accountField["type"];
            Assert.AreEqual(accountType["name"], "Account");
            Assert.AreEqual(accountType["kind"], "OBJECT");

            var accountPaidField = fields.First(f => (string) f["name"] == "accountPaid");
            var accountPaidType = (IDictionary<string, object>) accountPaidField["type"];
            Assert.AreEqual(accountPaidType["name"], "Boolean");
            Assert.AreEqual(accountPaidType["kind"], "SCALAR");
        }

        [Test]
        public void SchemaTypes()
        {
            var gql = MemContext.CreateDefaultContext();
            var schema = (IDictionary<string, object>) gql.ExecuteQuery("{ __schema { types { name, kind, interfaces { name } } } }")["__schema"];
            var types = (List<IDictionary<string, object>>) schema["types"];

            var intType = types.First(t => (string) t["name"] == "Int");
            Assert.AreEqual(intType["name"], "Int");
            Assert.AreEqual(intType["kind"], "SCALAR");
            Assert.AreEqual(((List<IDictionary<string, object>>)intType["interfaces"]).Count, 0);

            var userType = types.First(t => (string) t["name"] == "User");
            Assert.AreEqual(userType["name"], "User");
            Assert.AreEqual(userType["kind"], "OBJECT");
            Assert.AreEqual(((List<IDictionary<string, object>>)userType["interfaces"]).Count, 0);
        }
    }
}
