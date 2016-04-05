using System.Collections.Generic;
using System.Linq;
using GraphQL.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class InMemoryExecutionTests
    {
        [TestMethod] public void LookupSingleEntity() => GenericTests.LookupSingleEntity(MemContext.CreateDefaultContext());
        [TestMethod] public void AliasOneField() => GenericTests.AliasOneField(MemContext.CreateDefaultContext());
        [TestMethod] public void NestedEntity() => GenericTests.NestedEntity(MemContext.CreateDefaultContext());
        [TestMethod] public void NoUserQueryReturnsNull() => GenericTests.NoUserQueryReturnsNull(MemContext.CreateDefaultContext());
        [TestMethod] public void CustomFieldSubQuery() => GenericTests.CustomFieldSubQuery(MemContext.CreateDefaultContext());
        [TestMethod] public void CustomFieldSubQueryUsingContext() => GenericTests.CustomFieldSubQueryUsingContext(MemContext.CreateDefaultContext());
        [TestMethod] public void List() => GenericTests.List(MemContext.CreateDefaultContext());
        [TestMethod] public void ListTypeIsList() => GenericTests.ListTypeIsList(MemContext.CreateDefaultContext());
        [TestMethod] public void NestedEntityList() => GenericTests.NestedEntityList(MemContext.CreateDefaultContext());
        [TestMethod] public void PostField() => GenericTests.PostField(MemContext.CreateDefaultContext());
        [TestMethod] public void PostFieldSubQuery() => GenericTests.PostFieldSubQuery(MemContext.CreateDefaultContext());
        [TestMethod] public void TypeName() => GenericTests.TypeName(MemContext.CreateDefaultContext());

        [TestMethod]
        public void AddAllFields()
        {
            var schema = GraphQL<MemContext>.CreateDefaultSchema(() => new MemContext());
            schema.AddType<User>().AddAllFields();
            schema.AddType<Account>().AddAllFields();
            schema.AddQuery("user", new { id = 0 }, (db, args) => db.Users.AsQueryable().Where(u => u.Id == args.id).FirstOrDefault());
            schema.Complete();
            var gql = new GraphQL<MemContext>(schema);
            var user = (IDictionary<string, object>)gql.ExecuteQuery("query user(id:1) { id, name }")["data"];
            Assert.AreEqual(user["id"], 1);
            Assert.AreEqual(user["name"], "Joe User");
            Assert.AreEqual(user.Keys.Count, 2);
        }
    }
}
