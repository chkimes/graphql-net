# GraphQL.NET with Azure Cosmos DB example

This is an example of a real-world setup with Cosmos DB as the persistance layer.

## Run it yourself

 * Clone this repo locally.
 * Install the [Cosmos DB Emulator](https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator) or spin up your own Cosmos DB instance in Azure (at a cost).
 * Inside this instance, create a database called "mydb".
 * Inside this DB, create a collection with: id of "users", 10GB fixed size, minimum throughput and no partition key.
 * If you're not using the emulator, replace the CosmosDb settings in "appsettings.Development.json" with your own.
 * Build and run the application. It will seed data for you.