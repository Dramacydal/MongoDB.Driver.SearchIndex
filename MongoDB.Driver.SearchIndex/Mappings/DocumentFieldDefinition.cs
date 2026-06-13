using MongoDB.Bson;

namespace MongoDB.Driver.SearchIndex.Mappings;

public class DocumentFieldDefinition : SearchFieldDefinition
{
    public bool Dynamic { get; init; }
    public Dictionary<string, List<SearchFieldDefinition>>? Fields { get; init; }

    public override BsonDocument ToBsonDocument()
    {
        var doc = new BsonDocument
        {
            { "type", "document" },
            { "dynamic", Dynamic },
        };
        if (Fields != null && Fields.Count > 0)
        {
            var fields = new BsonDocument();
            foreach (var (path, defs) in Fields)
                fields[path] = defs.Count == 1
                    ? defs[0].ToBsonDocument()
                    : new BsonArray(defs.Select(d => d.ToBsonDocument()));
            doc["fields"] = fields;
        }
        return doc;
    }
}

public class EmbeddedDocumentsFieldDefinition : SearchFieldDefinition
{
    public bool Dynamic { get; init; }
    public Dictionary<string, List<SearchFieldDefinition>>? Fields { get; init; }

    public override BsonDocument ToBsonDocument()
    {
        var doc = new BsonDocument
        {
            { "type", "embeddedDocuments" },
            { "dynamic", Dynamic },
        };
        if (Fields != null && Fields.Count > 0)
        {
            var fields = new BsonDocument();
            foreach (var (path, defs) in Fields)
                fields[path] = defs.Count == 1
                    ? defs[0].ToBsonDocument()
                    : new BsonArray(defs.Select(d => d.ToBsonDocument()));
            doc["fields"] = fields;
        }
        return doc;
    }
}
