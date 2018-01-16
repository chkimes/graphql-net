# GraphQL.NET with Azure Cosmos DB example

This is an example of a real-world setup with Cosmos DB as the persistance layer.

## Run it yourself

* Clone this repo locally.
* Install the [Cosmos DB Emulator](https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator) or spin up your own Cosmos DB instance in Azure (at a cost).
* If you're not using the emulator, replace the CosmosDb settings in "appsettings.Development.json" with your own.
* Build and run the application. It will create in your instance a db, "users" collection (400RUs) and seed some users automatically for you.

## Backlog

* Seed process: make user documents more complex for better scenarios.
* Write a bunch of queries at API endpoints to test, or maybe just a generic endpoint that accepts the query as a string and runs it (e.g. for Postman usage)