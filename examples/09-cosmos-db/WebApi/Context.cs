using WebApi.Services;
using GraphQL.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApi
{
    public class Context
    {
        private readonly IMyDocumentClient documentClient;

        public Context(IMyDocumentClient documentClient)
        {
            this.documentClient = documentClient;
        }

        public IQueryable<Models.User> Users { get { return documentClient.GetUsersIQueryableAsync().Result; } set { } }
    }
}
