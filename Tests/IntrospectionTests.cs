using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class IntrospectionTests
    {
        [TestMethod]
        public void TypeName()
        {
            var gql = MemContext.CreateDefaultContext();
            var type = (IDictionary<string, object>)gql.ExecuteQuery("query __type(name: \"User\") { name }")["data"];
            Assert.AreEqual(type["name"], "User");
            Assert.AreEqual(type.Keys.Count, 1);
        }
    }
}
