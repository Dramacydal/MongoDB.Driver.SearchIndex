using MongoDB.Bson;

namespace MongoDB.Driver.SearchIndex.Mappings;

public class BooleanFieldDefinition : SearchFieldDefinition
{
    public override BsonDocument ToBsonDocument() => new("type", "boolean");
}

public class ObjectIdFieldDefinition : SearchFieldDefinition
{
    public override BsonDocument ToBsonDocument() => new("type", "objectId");
}

public class UuidFieldDefinition : SearchFieldDefinition
{
    public override BsonDocument ToBsonDocument() => new("type", "uuid");
}

public class ArrayFieldDefinition : SearchFieldDefinition
{
    public required SearchFieldDefinition ElementType { get; init; }

    public override BsonDocument ToBsonDocument() => ElementType.ToBsonDocument();
}
