using System;
using GraphQL.Net;
using NUnit.Framework;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace _01_simple_query
{
    [TestFixture]
    public class SimpleQueryExample
    {
        class Context
        {
            public Character Hero { get; set; }
        }

        class Character
        {
            public string Name { get; set; }
            public IEnumerable<Character> Friends { get; set; }
        }

        private Context CreateDefaultContext()
        {
            return new Context
            {
                Hero = new Character
                {
                    Name = "R2-D2",
                    Friends = new List<Character>
                        {
                            new Character {
                              Name = "Luke Skywalker"
                            },
                            new Character {
                              Name = "Han Solo"
                            },
                            new Character {
                              Name = "Leia Organa"
                            }
                        }
                }
            };
        }

        [Test]
        public void RunExample()
        {
            var schema = GraphQL<Context>.CreateDefaultSchema(CreateDefaultContext);
            schema.AddType<Character>().AddAllFields();
            schema.AddField("hero", c => c.Hero);

            schema.Complete();

            var gql = new GraphQL<Context>(schema);
            var queryResult = gql.ExecuteQuery(
                @"{
                    hero {
                        name,
                        friends {
                            name
                        }
                    }
                  }"
                );
            DeepEquals(
                queryResult,
                @"{
                    hero: {
                        name: 'R2-D2',
                        friends: [
                            {
                              name: 'Luke Skywalker'
                            },
                            {
                                name: 'Han Solo'
                            },
                            {
                                name: 'Leia Organa'
                            }
                        ]
                    }
                  }"
                );
        }

        private static readonly JsonSerializer Serializer = new JsonSerializer
        {
            Converters = { new StringEnumConverter() },
        };

        private static void DeepEquals(IDictionary<string, object> results, string json)
        {
            var expected = JObject.Parse(json);
            var actual = JObject.FromObject(results, Serializer);
            if (expected.ToString() == actual.ToString())
                return;

            throw new Exception($"Results do not match expectation:\n\nExpected:\n{expected}\n\nActual:\n{actual}");
        }
    }
}
