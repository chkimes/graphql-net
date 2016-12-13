using System;
using GraphQL.Net;
using NUnit.Framework;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Linq;

namespace _07_mutations
{
    [TestFixture]
    public class MutationsExample
    {
        class Context
        {
            public IList<Review> Reviews { get; set; }
        }

        public class Review
        {
            public string Episode { get; set; }
            public string Commentary { get; set; }
            public int Stars { get; set; }
            public int Id { get; internal set; }
        }

        public class ReviewInput
        {
            public string Commentary { get; set; }
            public int Stars { get; set; }
        }

        [Test]
        public void RunExample()
        {
            var defaultContext = new Context
            {
                Reviews = new List<Review> {
                    new Review {
                        Stars = 5,
                        Episode = "EMPIRE",
                        Commentary = "Great movie"
                    }
                }
            };

            var schema = GraphQL<Context>.CreateDefaultSchema(() => defaultContext);
            schema.AddType<Review>().AddAllFields();
            schema.AddScalar(
                new
                {
                    stars = default(int),
                    commentary = default(string)
                },
                i => new ReviewInput { Stars = i.stars, Commentary = i.commentary },
                "ReviewInput"
            );
            schema.AddMutation(
                "createReview",
                new { episode = "EMPIRE", review = default(ReviewInput) },
                (db, args) =>
                {
                    var newId = db.Reviews.Select(r => r.Id).Max() + 1;
                    var review = new Review
                    {
                        Id = newId,
                        Episode = args.episode,
                        Commentary = args.review.Commentary,
                        Stars = args.review.Stars
                    };
                    db.Reviews.Add(review);
                    return newId;
                },
                (db, args, rId) => db.Reviews.AsQueryable().SingleOrDefault(r => r.Id == rId)
                );
            schema.Complete();

            var gql = new GraphQL<Context>(schema);
            var queryResult = gql.ExecuteQuery(
                @"mutation CreateReviewForEpisode($ep: String!, $review: ReviewInput!) {
                    createReview(episode: ""JEDI"", review: {commentary: ""This is a great movie!"", stars: 5}) {
                        stars
                        commentary
                    }
                }"
                );
            DeepEquals(
                queryResult,
                @"{
                    ""createReview"": {
                        ""stars"": 5,
                        ""commentary"": ""This is a great movie!""
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