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
        public void EnumTypeDirectFields()
        {
            var gql = MemContext.CreateDefaultContext();
            var results = gql.ExecuteQuery("{ __type(name: \"AccountType\") { name, description, kind } }");
            Test.DeepEquals(results, "{ __type: { name: 'AccountType', description: null, kind: 'ENUM' } }");
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
        public void ChildFieldType()
        {
            var gql = MemContext.CreateDefaultContext();
            var results =  gql.ExecuteQuery("{ __type(name: \"User\") { fields { name, type { name, kind, ofType { name, kind } } } } }");
            Test.DeepEquals(results,
                @"{
                      __type: {
                          fields: [
                              { name: 'id', type: { name: null, kind: 'NON_NULL', ofType: { name: 'Int', kind: 'SCALAR' } } },
                              { name: 'name', type: { name: 'String', kind: 'SCALAR', ofType: null } },
                              { name: 'account', type: { name: 'Account', kind: 'OBJECT', ofType: null } },
                              { name: 'nullRef', type: { name: 'NullRef', kind: 'OBJECT', ofType: null } },
                             { name: 'total', type: { name: null, kind: 'NON_NULL', ofType: { name: 'Int', kind: 'SCALAR' } } },
                              { name: 'accountPaid', type: { name: null, kind: 'NON_NULL', ofType: { name: 'Boolean', kind: 'SCALAR' } } },
                              { name: 'abc', type: { name: 'String', kind: 'SCALAR', ofType: null } },
                              { name: 'sub', type: { name: 'Sub', kind: 'OBJECT', ofType: null } },
                              { name: '__typename', type: { name: 'String', kind: 'SCALAR', ofType: null } }
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

            Console.WriteLine(gql.ExecuteQuery("{ __schema { types { name, kind, interfaces { name } } } }")["__schema"]);
            var intType = types.First(t => (string) t["name"] == "Int");
            Assert.AreEqual(intType["name"], "Int");
            Assert.AreEqual(intType["kind"].ToString(), "SCALAR");
            Assert.IsNull(intType["interfaces"]);

            var userType = types.First(t => (string) t["name"] == "User");
            Assert.AreEqual(userType["name"], "User");
            Assert.AreEqual(userType["kind"].ToString(), "OBJECT");
            Assert.AreEqual(((List<IDictionary<string, object>>)userType["interfaces"]).Count, 0);
        }

        [Test]
        public void FieldArgsQuery()
        {
            var gql = MemContext.CreateDefaultContext();
            var results = gql.ExecuteQuery("{ __schema { queryType { fields { name, args { name, description, type { name, kind, ofType { name, kind } }, defaultValue } } } } }");
            
            Test.DeepEquals(
                results,
                @"{ 
                    ""__schema"": {
                        ""queryType"": {
                          ""fields"": [
                            {
                              ""name"": ""users"",
                              ""args"": []
                            },
                            {
                              ""name"": ""user"",
                              ""args"": [
                                {
                                  ""name"": ""id"",
                                  ""description"": null,
                                  ""type"": {
                                    ""name"": null,
                                    ""kind"": ""NON_NULL"",
                                    ""ofType"": {
                                      ""name"": ""Int"",
                                      ""kind"": ""SCALAR""
                                    }
                                  },
                                  ""defaultValue"": null
                                }
                              ]
                            },
                            {
                              ""name"": ""account"",
                              ""args"": [
                                {
                                  ""name"": ""id"",
                                  ""description"": null,
                                  ""type"": {
                                    ""name"": null,
                                    ""kind"": ""NON_NULL"",
                                    ""ofType"": {
                                      ""name"": ""Int"",
                                      ""kind"": ""SCALAR""
                                    }
                                  },
                                  ""defaultValue"": null
                                }
                              ]
                            },
                            {
                              ""name"": ""accountPaidBy"",
                              ""args"": [
                                {
                                  ""name"": ""paid"",
                                  ""description"": null,
                                  ""type"": {
                                    ""name"": ""DateTime"",
                                    ""kind"": ""INPUT_OBJECT"",
                                    ""ofType"": null
                                  },
                                  ""defaultValue"": null
                                }
                              ]
                            },
                            {
                              ""name"": ""accountsByGuid"",
                              ""args"": [
                                {
                                  ""name"": ""guid"",
                                  ""description"": null,
                                  ""type"": {
                                    ""name"": null,
                                    ""kind"": ""NON_NULL"",
                                    ""ofType"": {
                                      ""name"": ""Guid"",
                                      ""kind"": ""SCALAR""
                                    }
                                  },
                                  ""defaultValue"": null
                                }
                              ]
                            },
                            {
                              ""name"": ""accountsByType"",
                              ""args"": [
                                {
                                  ""name"": ""accountType"",
                                  ""description"": null,
                                  ""type"": {
                                    ""name"": null,
                                    ""kind"": ""NON_NULL"",
                                    ""ofType"": {
                                      ""name"": ""AccountType"",
                                      ""kind"": ""ENUM""
                                    }
                                  },
                                  ""defaultValue"": null
                                }
                              ]
                            },
                            {
                              ""name"": ""mutateMes"",
                              ""args"": [
                                {
                                  ""name"": ""id"",
                                  ""description"": null,
                                  ""type"": {
                                    ""name"": null,
                                    ""kind"": ""NON_NULL"",
                                    ""ofType"": {
                                      ""name"": ""Int"",
                                      ""kind"": ""SCALAR""
                                    }
                                  },
                                  ""defaultValue"": null
                                }
                              ]
                            },
                            {
                              ""name"": ""mutate"",
                              ""args"": [
                                {
                                  ""name"": ""id"",
                                  ""description"": null,
                                  ""type"": {
                                    ""name"": null,
                                    ""kind"": ""NON_NULL"",
                                    ""ofType"": {
                                      ""name"": ""Int"",
                                      ""kind"": ""SCALAR""
                                    }
                                  },
                                  ""defaultValue"": null
                                },
                                {
                                  ""name"": ""newVal"",
                                  ""description"": null,
                                  ""type"": {
                                    ""name"": null,
                                    ""kind"": ""NON_NULL"",
                                    ""ofType"": {
                                      ""name"": ""Int"",
                                      ""kind"": ""SCALAR""
                                    }
                                  },
                                  ""defaultValue"": null
                                }
                              ]
                            },
                            {
                              ""name"": ""addMutate"",
                              ""args"": [
                                {
                                  ""name"": ""newVal"",
                                  ""description"": null,
                                  ""type"": {
                                    ""name"": null,
                                    ""kind"": ""NON_NULL"",
                                    ""ofType"": {
                                      ""name"": ""Int"",
                                      ""kind"": ""SCALAR""
                                    }
                                  },
                                  ""defaultValue"": null
                                }
                              ]
                            },
                            {
                              ""name"": ""hero"",
                              ""args"": [
                                {
                                  ""name"": ""id"",
                                  ""description"": null,
                                  ""type"": {
                                    ""name"": null,
                                    ""kind"": ""NON_NULL"",
                                    ""ofType"": {
                                      ""name"": ""Int"",
                                      ""kind"": ""SCALAR""
                                    }
                                  },
                                  ""defaultValue"": null
                                }
                              ]
                            },
                            {
                              ""name"": ""heros"",
                              ""args"": []
                            },
                            {
                              ""name"": ""__schema"",
                              ""args"": []
                            },
                            {
                              ""name"": ""__type"",
                              ""args"": [
                                {
                                  ""name"": ""name"",
                                  ""description"": null,
                                  ""type"": {
                                    ""name"": ""String"",
                                    ""kind"": ""SCALAR"",
                                    ""ofType"": null
                                  },
                                  ""defaultValue"": null
                                }
                              ]
                            },
                            {
                              ""name"": ""__typename"",
                              ""args"": []
                            }
                          ]
                        }
                      }
                    }");
        }

        [Test]
        public void FullIntrospectionQuery()
        {
            var gql = MemContext.CreateDefaultContext();
            var results = gql.ExecuteQuery(
                @"query IntrospectionQuery {
                    __schema {
                      queryType { name }
                      mutationType { name }
                      subscriptionType { name }
                      types {
                        ...FullType
                      }
                      directives {
                        name
                        description
                        locations
                        args {
                          ...InputValue
                        }
                      }
                    }
                  }
                  fragment FullType on __Type {
                    kind
                    name
                    description
                    fields(includeDeprecated: true) {
                      name
                      description
                      args {
                        ...InputValue
                      }
                      type {
                        ...TypeRef
                      }
                      isDeprecated
                      deprecationReason
                    }
                    inputFields {
                      ...InputValue
                    }
                    interfaces {
                      ...TypeRef
                    }
                    enumValues(includeDeprecated: true) {
                      name
                      description
                      isDeprecated
                      deprecationReason
                    }
                    possibleTypes {
                      ...TypeRef
                    }
                  }
                  fragment InputValue on __InputValue {
                    name
                    description
                    type { ...TypeRef }
                    defaultValue
                  }
                  fragment TypeRef on __Type {
                    kind
                    name
                    ofType {
                      kind
                      name
                      ofType {
                        kind
                        name
                        ofType {
                          kind
                          name
                          ofType {
                            kind
                            name
                            ofType {
                              kind
                              name
                              ofType {
                                kind
                                name
                                ofType {
                                  kind
                                  name
                                }
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                "
                );
            // Must not throw
            // TODO: Add assertions
        }
    }
}
