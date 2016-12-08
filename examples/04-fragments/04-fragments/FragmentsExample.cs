using System;
using GraphQL.Net;
using NUnit.Framework;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Linq;

namespace _04_fragments
{
    [TestFixture]
    public class FragmentsExample
    {
        class Context
        {
            public IList<Character> Heros { get; set; }
        }

        public class Character
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Episode { get; set; }
            public string[] AppearsIn { get; set; }
            public IEnumerable<Character> Friends { get; set; }
        }

        [Test]
        public void RunExample()
        {
            var schema = GraphQL<Context>.CreateDefaultSchema(() =>
            new Context
            {
                Heros = new List<Character> {
                    new Character {
                        Id = "1000",
                        Name = "Luke Skywalker",
                        Episode = "EMPIRE",
                        AppearsIn = new string[] { "NEWHOPE", "EMPIRE", "JEDI"},
                        Friends = new List<Character> {
                            new Character { Name = "Han Solo"},
                            new Character { Name = "Leia Organa"},
                            new Character { Name = "C-3PO"},
                            new Character { Name = "R2-D2"}
                        }
                    },
                    new Character {
                        Id = "1001",
                        Name = "R2-D2",
                        Episode = "JEDI",
                        AppearsIn = new string[] {"NEWHOPE", "EMPIRE", "JEDI" },
                        Friends = new List<Character> {
                            new Character { Name = "Luke Skywalker"},
                            new Character { Name = "Han Solo"},
                            new Character { Name = "Leia Organa"}
                        }
                    }
                }
            });
            schema.AddType<Character>().AddAllFields();
            schema.AddField(
                "hero",
                new { episode = "EMPIRE" },
                (c, args) => c.Heros.SingleOrDefault(h => h.Episode == args.episode));

            schema.Complete();

            var gql = new GraphQL<Context>(schema);
            var queryResult = gql.ExecuteQuery(
                @"{
                      leftComparison: hero(episode: ""EMPIRE"") {
                        ...comparisonFields
                      }
                      rightComparison: hero(episode: ""JEDI"") {
                        ...comparisonFields
                      }
                  }
                  fragment comparisonFields on Character {
                    name
                    appearsIn
                    friends {
                        name
                    }
                  }"
                );
            DeepEquals(
                queryResult, 
                @"{
                    ""leftComparison"": {
                        ""name"": ""Luke Skywalker"",
                        ""appearsIn"": [
                            ""NEWHOPE"",
                            ""EMPIRE"",
                            ""JEDI""
                        ],
                        ""friends"": [
                            {
                              ""name"": ""Han Solo""
                            },
                            {
                              ""name"": ""Leia Organa""
                            },
                            {
                              ""name"": ""C-3PO""
                            },
                            {
                              ""name"": ""R2-D2""
                            }
                        ]
                    },
                    ""rightComparison"": {
                      ""name"": ""R2-D2"",
                      ""appearsIn"": [
                        ""NEWHOPE"",
                        ""EMPIRE"",
                        ""JEDI""
                      ],
                      ""friends"": [
                        {
                          ""name"": ""Luke Skywalker""
                        },
                        {
                          ""name"": ""Han Solo""
                        },
                        {
                          ""name"": ""Leia Organa""
                        }
                      ]
                    }
                  }
                ");
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