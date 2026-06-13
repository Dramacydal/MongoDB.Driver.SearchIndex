using MongoDB.Bson;

namespace MongoDB.Driver.SearchIndex.Mappings;

public class DateFieldDefinition : SearchFieldDefinition
{
    public override BsonDocument ToBsonDocument() => new("type", "date");
}

/// <remarks>Deprecated. Use <see cref="DateFieldDefinition"/> instead.</remarks>
public class DateFacetFieldDefinition : SearchFieldDefinition
{
    public override BsonDocument ToBsonDocument() => new("type", "dateFacet");
}
