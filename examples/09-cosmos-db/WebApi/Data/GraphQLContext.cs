using System.Linq;
using WebApi.Services;

namespace WebApi.Data
{
    public class GraphQLContext
    {
        private readonly IMyDocumentClient documentClient;

        public GraphQLContext(IMyDocumentClient documentClient)
        {
            this.documentClient = documentClient;
        }

        public IQueryable<Models.User> Users
        {
            get
            {
                return documentClient.GetUsers();
            }
        }

        public IQueryable<Models.Account> Accounts
        {
            get
            {
                return documentClient.GetAccounts();
            }
        }
    }
}
