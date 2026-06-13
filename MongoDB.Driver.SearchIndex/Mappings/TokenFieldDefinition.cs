using MongoDB.Bson;

namespace MongoDB.Driver.SearchIndex.Mappings;

public enum TokenNormalizer
{
    None,
    Lowercase,
}

public class TokenFieldDefinition : SearchFieldDefinition
{
    public TokenNormalizer Normalizer { get; init; } = TokenNormalizer.None;

    public override BsonDocument ToBsonDocument()
    {
        var doc = new BsonDocument("type", "token");
        if (Normalizer != TokenNormalizer.None)
            doc["normalizer"] = "lowercase";
        return doc;
    }
}

/// <remarks>Deprecated. Use <see cref="TokenFieldDefinition"/> instead.</remarks>
public class StringFacetFieldDefinition : SearchFieldDefinition
{
    public override BsonDocument ToBsonDocument() => new("type", "stringFacet");
}
