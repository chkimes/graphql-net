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
            await CreateAccounts();
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

        private async Task CreateAccounts()
        {
            // Create accounts collection
            await documentClient.GetOrCreateAccountsCollectionAsync();

            // Insert accounts
            foreach (var accountToInsert in Accounts)
            {
                var user = await documentClient.Current.CreateDocumentAsync(
                    documentClient.AccountsCollectionSelfLink,
                    accountToInsert
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
            },
            AccountId = "1"
        };

        public static User User2 = new User()
        {
            Id = "9c0b2900-6566-4632-a910-030ed9edced1",
            Profile = new Profile()
            {
                Age = 20,
                Gender = "male"
            },
            AccountId = "2"
        };

        public static User User3 = new User()
        {
            Id = "a03aa638-f93f-4547-9428-c00333d3262e",
            Profile = new Profile()
            {
                Age = 30,
                Gender = "female"
            },
            AccountId = "3"
        };

        public static User[] Users = new []
        {
            User1,
            User2,
            User3
        };

        public static Account Account1 = new Account()
        {
            Id = "1",
            Name = "Account 1",
            Paid = false
        };

        public static Account Account2 = new Account()
        {
            Id = "2",
            Name = "Account 2",
            Paid = true
        };

        public static Account Account3 = new Account()
        {
            Id = "3",
            Name = "Account 3",
            Paid = false
        };

        public static Account[] Accounts = new[]
        {
            Account1,
            Account2,
            Account3
        };
    }
}
