using System;
using GraphQL.Net;
using NUnit.Framework;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Linq;

namespace _02_field_arguments
{
    [TestFixture]
    public class FieldArgumentsExample
    {
        class Context
        {
            public IList<Human> Humans { get; set; }
        }

        class Human
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public double Height { get; set; }
        }

        [Test]
        public void RunExample()
        {
            var schema = GraphQL<Context>.CreateDefaultSchema(() =>
            new Context
            {
                Humans = new List<Human> {
                    new Human { Id = "1000", Name = "Luke Skywalker", Height = 1.72 }
                }
            });
            schema.AddType<Human>().AddAllFields();
            schema.AddField(
                "human",
                new { id = "-1" },
                (c, args) => c.Humans.SingleOrDefault(h => h.Id == args.id));

            schema.Complete();

            var gql = new GraphQL<Context>(schema);
            var queryResult = gql.ExecuteQuery("{human(id: \"1000\") {name, height}}");
            DeepEquals(queryResult, "{human: {name: 'Luke Skywalker', height: 1.72}}");
        }

        [Test]
        public void RunExample2()
        {
            var schema = GraphQL<Context>.CreateDefaultSchema(() =>
            new Context
            {
                Humans = new List<Human> {
                    new Human { Id = "1000", Name = "Luke Skywalker", Height = 1.72 }
                }
            });
            var humanSchema = schema.AddType<Human>();
            humanSchema.AddField(h => h.Id);
            humanSchema.AddField(h => h.Name);
            humanSchema.AddField(
                "height", 
                new { unit = "METER"}, 
                (c, args, h) => args.unit == "FOOT" ? h.Height * 3.28084 : h.Height);

            schema.AddField(
                "human",
                new { id = "-1" },
                (c, args) => c.Humans.SingleOrDefault(h => h.Id == args.id));

            schema.Complete();

            var gql = new GraphQL<Context>(schema);
            var queryResult = gql.ExecuteQuery("{human(id: \"1000\") {name, height(unit: \"FOOT\")}}");
            DeepEquals(queryResult, "{human: {name: 'Luke Skywalker', height: 5.6430448}}");
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