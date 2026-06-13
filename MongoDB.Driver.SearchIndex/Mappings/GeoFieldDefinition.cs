using MongoDB.Bson;

namespace MongoDB.Driver.SearchIndex.Mappings;

public class GeoFieldDefinition : SearchFieldDefinition
{
    public bool IndexShapes { get; init; } = false;

    public override BsonDocument ToBsonDocument()
    {
        var doc = new BsonDocument("type", "geo");
        if (IndexShapes) doc["indexShapes"] = true;
        return doc;
    }
}
