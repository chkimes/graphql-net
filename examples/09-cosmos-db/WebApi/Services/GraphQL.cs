using WebApi.Models;
using GraphQL.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApi.Services
{
    public interface IGraphQL
    {
        GraphQL<Context> Current { get; }
    }

    public class GraphQL : IGraphQL
    {
        private readonly IMyDocumentClient documentClient;

        public GraphQL<Context> Current { get; private set; }

        public GraphQL(IMyDocumentClient documentClient)
        {
            this.documentClient = documentClient;

            // Build schema
            var schema = GraphQL<Context>.CreateDefaultSchema(() => new Context(documentClient));

            var user = schema.AddType<User>();
            user.AddAllFields();

            schema.AddField("totalUsers", db => db.Users.Count());
            schema.AddListField("users", db => db.Users);
            schema.AddField("user", new { id = "" }, (db, args) => db.Users.Where(u => u.Id == args.id).ToArray().FirstOrDefault());

            schema.Complete();

            // Initialise singleton GraphQL instance
            this.Current = new GraphQL<Context>();
        }
    }
}
