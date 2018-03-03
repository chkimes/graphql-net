using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace WebApi.Services
{
    public interface IMyDocumentClient
    {
        IDocumentClient Current { get; }

        string DatabaseSelfLink { get; }
        Task<Database> GetOrCreateDatabaseAsync();

        string UsersCollectionSelfLink { get; }
        Task<DocumentCollection> GetOrCreateUsersCollectionAsync();
        IQueryable<Models.User> GetUsers();

        // etc
    }

    public class MyDocumentClient : IMyDocumentClient
    {
        private readonly string endpointUrl;
        private readonly string authKey;

        private readonly string databaseId;
        public string DatabaseSelfLink { get; private set; }

        private readonly string usersCollectionId = "users";
        public string UsersCollectionSelfLink { get; private set; }

        private static readonly ConnectionPolicy connPolicy = new ConnectionPolicy();
        //{
        //    ConnectionMode = ConnectionMode.Direct,
        //    ConnectionProtocol = Protocol.Tcp
        //};

        public IDocumentClient Current { get; private set; }

        public MyDocumentClient(IConfiguration config)
        {
            endpointUrl = config["CosmosDb:EndpointUrl"];
            authKey = config["CosmosDb:AuthorizationKey"];
            databaseId = config["CosmosDb:DatabaseId"];

            Current = new DocumentClient(new Uri(endpointUrl), authKey, connectionPolicy: connPolicy);

            // Setup
            Task.WaitAll(GetOrCreateDatabaseAsync());
            Task.WaitAll(new [] {
                GetOrCreateUsersCollectionAsync(),
                // etc
            });
        }
        
        public async Task<Database> GetOrCreateDatabaseAsync()
        {
            var db = Current.CreateDatabaseQuery().Where(d => d.Id == databaseId).ToArray().FirstOrDefault()
                ?? await Current.CreateDatabaseAsync(new Database { Id = databaseId });
            DatabaseSelfLink = db.SelfLink;
            return db;
        }
        
        public async Task<DocumentCollection> GetOrCreateUsersCollectionAsync()
        {
            var usersColl = Current.CreateDocumentCollectionQuery(DatabaseSelfLink).Where(c => c.Id == usersCollectionId).ToArray().FirstOrDefault()
                ?? await Current.CreateDocumentCollectionAsync(DatabaseSelfLink, new DocumentCollection() { Id = usersCollectionId }, new RequestOptions() { OfferThroughput = 400 });
            UsersCollectionSelfLink = usersColl.SelfLink;
            return usersColl;
        }

        public IQueryable<Models.User> GetUsers()
        {
            return Current.CreateDocumentQuery<Models.User>(UsersCollectionSelfLink);
        }

    }
}
