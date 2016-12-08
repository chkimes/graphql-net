using System;
using GraphQL.Net;
using NUnit.Framework;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Linq;

namespace _03_aliases
{
    [TestFixture]
    public class AliasesExample
    {
        class Context
        {
            public IList<Hero> Heros { get; set; }
        }

        class Hero
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Episode { get; set; }
        }

        [Test]
        public void RunExample()
        {
            var schema = GraphQL<Context>.CreateDefaultSchema(() =>
            new Context
            {
                Heros = new List<Hero> {
                    new Hero { Id = "1000", Name = "Luke Skywalker", Episode = "EMPIRE" },
                    new Hero { Id = "1001", Name = "R2-D2", Episode = "JEDI" }
                }
            });
            schema.AddType<Hero>().AddAllFields();
            schema.AddField(
                "hero",
                new { episode = "EMPIRE" },
                (c, args) => c.Heros.SingleOrDefault(h => h.Episode == args.episode));

            schema.Complete();

            var gql = new GraphQL<Context>(schema);
            var queryResult = gql.ExecuteQuery("{empireHero: hero(episode: \"EMPIRE\") {name}, jediHero: hero(episode: \"JEDI\") {name}}");
            DeepEquals(queryResult, "{empireHero: {name: 'Luke Skywalker'}, jediHero: {name: 'R2-D2'}}");
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