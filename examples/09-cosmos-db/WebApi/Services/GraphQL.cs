﻿using GraphQL.Net;
using System.Linq;
using WebApi.Data;
using WebApi.Models;

namespace WebApi.Services
{
    public interface IGraphQL
    {
        GraphQL<GraphQLContext> Current { get; }
    }

    public class GraphQL : IGraphQL
    {
        private readonly IMyDocumentClient documentClient;

        public GraphQL<GraphQLContext> Current { get; private set; }

        public GraphQL(IMyDocumentClient documentClient)
        {
            this.documentClient = documentClient;

            // Build schema
            var schema = GraphQL<GraphQLContext>.CreateDefaultSchema(() => new GraphQLContext(documentClient));

            // schema.AddType<Profile>().AddAllFields();
            schema.AddType<User>().AddAllFields();
            schema.AddListField("users", db => db.Users);
            schema.AddField("user", new { id = "" }, (db, args) => db.Users.Where(u => u.Id == args.id).ToArray().FirstOrDefault());

            schema.AddType<Account>().AddAllFields();
            schema.AddListField("accounts", db => db.Accounts);
            schema.AddField("account", new { id = "" }, (db, args) => db.Accounts.Where(a => a.Id == args.id).ToArray().FirstOrDefault());

            schema.Complete();

            // Initialise singleton GraphQL instance
            this.Current = new GraphQL<GraphQLContext>();
        }
    }
}
