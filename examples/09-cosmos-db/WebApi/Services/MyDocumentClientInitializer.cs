using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApi.Models;

namespace WebApi.Services
{
    public interface IMyDocumentClientInitializer
    {
        Task Reset();
    }

    public class MyDocumentClientInitializer : IMyDocumentClientInitializer
    {
        private readonly IMyDocumentClient documentClient;

        public MyDocumentClientInitializer(IMyDocumentClient documentClient)
        {
            this.documentClient = documentClient;
        }

        public async Task Reset()
        {
            await DeleteDatabase();
            await CreateDatabase();
            await CreateUsers();
        }

        private async Task DeleteDatabase()
        {
            await documentClient.Current.DeleteDatabaseAsync(documentClient.DatabaseSelfLink);
        }

        private async Task CreateDatabase()
        {
            await documentClient.GetOrCreateDatabaseAsync();
        }

        private async Task CreateUsers()
        {
            // Create users collection
            await documentClient.GetOrCreateUsersCollectionAsync();

            // Insert users
            foreach (var userToInsert in Users)
            {
                var user = await documentClient.Current.CreateDocumentAsync(
                    documentClient.UsersCollectionSelfLink,
                    userToInsert
                );
            }
        }

        public static User User1 = new User()
        {
            Id = "56745d3f-9812-42b2-b52a-c97e720f10ac",
            Profile = new Profile()
            {
                Age = 10,
                Gender = "female"
            }
        };

        public static User User2 = new User()
        {
            Id = "9c0b2900-6566-4632-a910-030ed9edced1",
            Profile = new Profile()
            {
                Age = 20,
                Gender = "male"
            }
        };

        public static User User3 = new User()
        {
            Id = "a03aa638-f93f-4547-9428-c00333d3262e",
            Profile = new Profile()
            {
                Age = 30,
                Gender = "female"
            }
        };

        public static User[] Users = new []
        {
            User1,
            User2,
            User3
        };
    }
}
