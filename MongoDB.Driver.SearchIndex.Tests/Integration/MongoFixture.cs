using MongoDB.Bson;
using Xunit;

namespace MongoDB.Driver.SearchIndex.Tests.Integration;

public class MongoFixture : IAsyncLifetime
{
    private IMongoClient? _client;
    private IMongoDatabase? _database;

    public string? ConnectionString { get; private set; }
    public bool IsAvailable => _database != null;

    public async Task InitializeAsync()
    {
        ConnectionString = MongoConnectionString.Value;
        if (ConnectionString == null) return;

        _client = new MongoClient(ConnectionString);
        var dbName = $"search_tests_{DateTime.UtcNow:yyyyMMddHHmmss}";
        _database = _client.GetDatabase(dbName);
    }

    public async Task DisposeAsync()
    {
        if (_database != null)
            await _client!.DropDatabaseAsync(_database.DatabaseNamespace.DatabaseName);
    }

    public IMongoCollection<BsonDocument> GetCollection(string name)
        => _database!.GetCollection<BsonDocument>(name);

    public static async Task WaitForIndexReady(
        IMongoCollection<BsonDocument> collection,
        string indexName,
        int timeoutSeconds = 120)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            var indexes = await collection.SearchIndexes.GetSearchIndexesAsync(indexName);
            if (indexes.Any(x => x.Name == indexName && x.Status == SearchIndexStatus.Ready && x.Queryable))
                return;
            await Task.Delay(2000);
        }
        throw new TimeoutException($"Index '{indexName}' did not become ready within {timeoutSeconds}s");
    }
}
