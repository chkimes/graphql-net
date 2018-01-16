using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;


namespace WebApi.Services
{
    public interface IMyDocumentClient
    {
        IDocumentClient Current { get; }
        Task<Database> GetDatabaseAsync();
        Task<DocumentCollection> GetUsersCollectionAsync();
        Task<IQueryable<Models.User>> GetUsersIQueryableAsync();
    }

    public class MyDocumentClient : IMyDocumentClient
    {
        // private readonly IConfiguration config;

        private readonly string endpointUrl;
        private readonly string databaseId;
        private readonly string authKey;
        private static readonly ConnectionPolicy connPolicy = new ConnectionPolicy()
        {
            ConnectionMode = ConnectionMode.Direct,
            ConnectionProtocol = Protocol.Tcp
        };

        public IDocumentClient Current { get; private set; }

        public MyDocumentClient(IConfiguration config)
        {
            // this.config = config;

            endpointUrl = config["CosmosDb:EndpointUrl"];
            databaseId = config["CosmosDb:DatabaseId"];
            authKey = config["CosmosDb:AuthorizationKey"];

            Current = new DocumentClient(new Uri(endpointUrl), authKey, connectionPolicy: connPolicy);
        }

        public async Task<Database> GetDatabaseAsync()
        {
            return Current.CreateDatabaseQuery().Where(db => db.Id == databaseId).ToArray().FirstOrDefault() 
                ?? await Current.CreateDatabaseAsync(new Database { Id = databaseId });
        }

        public async Task<DocumentCollection> GetUsersCollectionAsync()
        {
            var db = await GetDatabaseAsync();
            return Current.CreateDocumentCollectionQuery(db.SelfLink).Where(coll => coll.Id == "users").ToArray().FirstOrDefault();
        }

        public async Task<IQueryable<Models.User>> GetUsersIQueryableAsync()
        {
            var usersCollection = await GetUsersCollectionAsync();
            return Current.CreateDocumentQuery<Models.User>(usersCollection.SelfLink);
        }
    }
}
