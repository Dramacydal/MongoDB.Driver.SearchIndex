using MongoDB.Bson;

namespace MongoDB.Driver.SearchIndex.Mappings;

public enum NumberRepresentation
{
    Double,
    Int64,
}

public class NumberFieldDefinition : SearchFieldDefinition
{
    public NumberRepresentation Representation { get; init; } = NumberRepresentation.Double;
    public bool IndexIntegers { get; init; } = true;
    public bool IndexDoubles { get; init; } = true;

    public override BsonDocument ToBsonDocument()
    {
        var doc = new BsonDocument("type", "number");
        doc["representation"] = Representation == NumberRepresentation.Int64 ? "int64" : "double";
        if (!IndexIntegers) doc["indexIntegers"] = false;
        if (!IndexDoubles)  doc["indexDoubles"]  = false;
        return doc;
    }
}

/// <remarks>Deprecated. Use <see cref="NumberFieldDefinition"/> instead.</remarks>
public class NumberFacetFieldDefinition : SearchFieldDefinition
{
    public NumberRepresentation Representation { get; init; } = NumberRepresentation.Double;
    public bool IndexIntegers { get; init; } = true;
    public bool IndexDoubles { get; init; } = true;

    public override BsonDocument ToBsonDocument()
    {
        var doc = new BsonDocument("type", "numberFacet");
        doc["representation"] = Representation == NumberRepresentation.Int64 ? "int64" : "double";
        if (!IndexIntegers) doc["indexIntegers"] = false;
        if (!IndexDoubles)  doc["indexDoubles"]  = false;
        return doc;
    }
}
