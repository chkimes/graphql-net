using System;
using GraphQL.Net;
using NUnit.Framework;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Linq;

namespace _08_inline_fragments
{
    [TestFixture]
    public class InlineFragmentsExample
    {
        class Context
        {
            public IList<Character> Heros { get; set; }
        }

        class Character
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        class Human : Character
        {
            public double Height { get; set; }
        }
        class Stormtrooper : Human
        {
            public string Specialization { get; set; }
        }

        class Droid : Character
        {
            public string PrimaryFunction { get; set; }
        }

        [Test]
        public void RunExample()
        {
            var defaultContext = new Context
                {
                    Heros = new List<Character> {
                new Human
                {
                    Id = 1,
                    Name = "Han Solo",
                    Height = 5.6430448
                },
                new Stormtrooper
                {
                    Id = 2,
                    Name = "FN-2187",
                    Height = 4.9,
                    Specialization = "Imperial Snowtrooper"
                },
                new Droid
                {
                    Id = 3,
                    Name = "R2-D2",
                    PrimaryFunction = "Astromech"
                }
            }
            };

            var schema = GraphQL<Context>.CreateDefaultSchema(() => defaultContext);
            schema.AddType<Character>().AddAllFields();
            schema.AddType<Human>().AddAllFields();
            schema.AddType<Stormtrooper>().AddAllFields();
            schema.AddType<Droid>().AddAllFields();
            schema.AddListField(
                "heros",
                db => db.Heros.AsQueryable()
                );
            schema.Complete();

            var gql = new GraphQL<Context>(schema);
            var queryResult = gql.ExecuteQuery(
                @"query Heros {
                    heros {
                        name
                        ... on Droid {
                            primaryFunction
                        }
                        ... on Stormtrooper {
                            specialization
                        }
                        ... on Human {
                            height
                        }
                    }
                }"
                );
            DeepEquals(
                queryResult,
                @"{
                  ""heros"": [
                    {
                      ""name"": ""Han Solo"",
                      ""height"": 5.6430448
                    },
                    {
                      ""name"": ""FN-2187"",
                      ""specialization"": ""Imperial Snowtrooper"",
                      ""height"": 4.9
                    },
                    {
                      ""name"": ""R2-D2"",
                      ""primaryFunction"": ""Astromech""
                    }
                  ]
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