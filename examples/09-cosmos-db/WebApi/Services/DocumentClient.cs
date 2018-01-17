using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace WebApi.Services
{
    public interface IMyDocumentClient
    {
        IDocumentClient Current { get; }
        Dictionary<string, string> CollectionSelfLinks { get; }
    }

    public class MyDocumentClient : IMyDocumentClient
    {
        // private readonly IConfiguration config;

        private readonly string endpointUrl;
        private readonly string authKey;
        private readonly string databaseId;
        private readonly string usersCollectionId = "users";
        private static readonly ConnectionPolicy connPolicy = new ConnectionPolicy()
        {
            ConnectionMode = ConnectionMode.Direct,
            ConnectionProtocol = Protocol.Tcp
        };

        public IDocumentClient Current { get; private set; }

        private string databaseSelfLink;
        public Dictionary<string, string> CollectionSelfLinks { get; private set; }

        public MyDocumentClient(IConfiguration config)
        {
            // this.config = config;

            endpointUrl = config["CosmosDb:EndpointUrl"];
            authKey = config["CosmosDb:AuthorizationKey"];
            databaseId = config["CosmosDb:DatabaseId"];

            Current = new DocumentClient(new Uri(endpointUrl), authKey, connectionPolicy: connPolicy);

            Seed();
            Reflect();
        }

        private async void Reflect()
        {
            // Store db selfLink
            var database = Current.CreateDatabaseQuery().Where(db => db.Id == databaseId).ToArray().FirstOrDefault();
            databaseSelfLink = database.SelfLink;

            // Store all collection selfLink
            var collections = Current.CreateDocumentCollectionQuery(database.SelfLink).Where(coll => coll.Id == usersCollectionId).ToArray();
            CollectionSelfLinks = new Dictionary<string, string>();
            foreach (var collection in collections)
            {
                CollectionSelfLinks.Add(collection.Id, collection.SelfLink);
            }
        }
        
        #region Seed stuff

        private async Task<Database> GetOrCreateDatabaseAsync()
        {
            return Current.CreateDatabaseQuery().Where(db => db.Id == databaseId).ToArray().FirstOrDefault() 
                ?? await Current.CreateDatabaseAsync(new Database { Id = databaseId });
        }

        private async Task<DocumentCollection> GetOrCreateUsersCollectionAsync()
        {
            var db = await GetOrCreateDatabaseAsync();
            return Current.CreateDocumentCollectionQuery(db.SelfLink).Where(coll => coll.Id == usersCollectionId).ToArray().FirstOrDefault()
                ?? await Current.CreateDocumentCollectionAsync(db.SelfLink, new DocumentCollection() { Id = usersCollectionId }, new RequestOptions() { OfferThroughput = 400 });
        }

        private async Task<IQueryable<Models.User>> GetUsersIQueryableAsync()
        {
            var usersColl = await GetOrCreateUsersCollectionAsync();
            return Current.CreateDocumentQuery<Models.User>(usersColl.SelfLink);
        }

        private async void Seed()
        {
            var users = await GetUsersIQueryableAsync();
            if (users.Count() < 1)
            {
                // Create some users
                var usersCollectionLink = UriFactory.CreateDocumentCollectionUri(databaseId, usersCollectionId);

                var usersToInsert = new List<Models.User>
                {
                    // TODO: Expand the user properties out to be more realistic and use all sorts of types (e.g. enums)
                    new Models.User()
                    {
                        Profile = new Models.Profile()
                        {
                            Age = 10,
                            Gender = "female"
                        }
                    },
                    new Models.User()
                    {
                        Profile = new Models.Profile()
                        {
                            Age = 20,
                            Gender = "male"
                        }
                    },
                    new Models.User()
                    {
                        Profile = new Models.Profile()
                        {
                            Age = 30,
                            Gender = "female"
                        }
                    },
                    new Models.User()
                    {
                        Profile = new Models.Profile()
                        {
                            Age = 40,
                            Gender = "female"
                        }
                    },
                    new Models.User()
                    {
                        Profile = new Models.Profile()
                        {
                            Age = 66,
                            Gender = "male"
                        }
                    }
                };

                foreach (var userToInsert in usersToInsert)
                {
                    var user = await Current.CreateDocumentAsync(usersCollectionLink, userToInsert);
                }
            }
        }

        #endregion
    }
}
