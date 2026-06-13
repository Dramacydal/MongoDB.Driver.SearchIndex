using MongoDB.Bson;
using MongoDB.Driver.Search;

namespace MongoDB.Driver.SearchIndex;

public static class SearchExtensions
{
    extension(IMongoSearchIndexManager manager)
    {
        public string CreateOne(string name, SearchIndexDefinition definition, SearchIndexType? type = null) =>
            manager.CreateOne(new CreateSearchIndexModel(name, type, definition.ToBsonDocument()));

        public Task<string> CreateOneAsync(string name, SearchIndexDefinition definition, SearchIndexType? type = null, CancellationToken ct = default) =>
            manager.CreateOneAsync(new CreateSearchIndexModel(name, type, definition.ToBsonDocument()), ct);

        public List<SearchIndexInfo> GetSearchIndexes(string? name = null)
        {
            var cursor = manager.List(name);
            return cursor.ToList().Select(ParseIndexInfo).ToList();
        }

        public async Task<List<SearchIndexInfo>> GetSearchIndexesAsync(string? name = null, CancellationToken ct = default)
        {
            var cursor = await manager.ListAsync(name, cancellationToken: ct);
            var results = new List<SearchIndexInfo>();
            await cursor.ForEachAsync(doc => results.Add(ParseIndexInfo(doc)), ct);
            return results;
        }

        public void UpdateOne(string name, SearchIndexDefinition definition, CancellationToken ct = default) =>
            manager.Update(name, definition.ToBsonDocument(), ct);

        public Task UpdateOneAsync(string name, SearchIndexDefinition definition, CancellationToken ct = default) =>
            manager.UpdateAsync(name, definition.ToBsonDocument(), ct);
    }

    private static SearchIndexInfo ParseIndexInfo(BsonDocument doc) => new()
    {
        Id = doc.GetValue("id", "").AsString,
        Name = doc.GetValue("name", "").AsString,
        Type = Enum.TryParse<SearchIndexType>(doc.GetValue("type", "search").AsString, true, out var indexType)
            ? indexType
            : SearchIndexType.Search,
        Status = Enum.TryParse<SearchIndexStatus>(doc.GetValue("status", "").AsString, true, out var status)
            ? status
            : SearchIndexStatus.Unknown,
        Queryable = doc.GetValue("queryable", false).AsBoolean,
        LatestVersion = doc.GetValue("latestVersion", 0).AsInt32,
        LatestDefinition = doc.Contains("latestDefinition")
            ? SearchIndexDefinition.Parse(doc["latestDefinition"].AsBsonDocument)
            : null
    };
}
