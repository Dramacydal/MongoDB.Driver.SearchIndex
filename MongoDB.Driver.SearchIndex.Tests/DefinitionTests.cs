using MongoDB.Bson;
using Xunit;

namespace MongoDB.Driver.SearchIndex.Tests;

public class DefinitionTests
{
    [Fact]
    public void Dynamic_Serializes_WithDynamicTrue()
    {
        var doc = SearchIndexDefinition.Dynamic().ToBsonDocument();
        Assert.True(doc["mappings"]["dynamic"].AsBoolean);
    }

    [Fact]
    public void Static_Serializes_WithDynamicFalse()
    {
        var doc = SearchIndexDefinition.Static().ToBsonDocument();
        Assert.False(doc["mappings"]["dynamic"].AsBoolean);
    }

    [Fact]
    public void Dynamic_HasNoFieldsSection()
    {
        var doc = SearchIndexDefinition.Dynamic().ToBsonDocument();
        Assert.False(doc["mappings"].AsBsonDocument.Contains("fields"));
    }

    [Fact]
    public void Static_WithFields_HasFieldsSection()
    {
        var doc = SearchIndexDefinition.Static().StringField("title").ToBsonDocument();
        Assert.True(doc["mappings"]["fields"].AsBsonDocument.Contains("title"));
    }

    [Fact]
    public void MultipleFieldsOnSamePath_SerializesAsArray()
    {
        var doc = SearchIndexDefinition.Static()
            .StringField("title")
            .AutocompleteField("title")
            .ToBsonDocument();

        var titleValue = doc["mappings"]["fields"]["title"];
        Assert.IsType<BsonArray>(titleValue);
        Assert.Equal(2, titleValue.AsBsonArray.Count);
    }

    [Fact]
    public void Dynamic_RoundTrip()
    {
        var parsed = SearchIndexDefinition.Parse(SearchIndexDefinition.Dynamic().ToBsonDocument());
        Assert.NotNull(parsed);
        Assert.True(parsed.IsDynamic);
    }

    [Fact]
    public void Static_RoundTrip()
    {
        var original = SearchIndexDefinition.Static().StringField("title");
        var parsed = SearchIndexDefinition.Parse(original.ToBsonDocument());
        Assert.NotNull(parsed);
        Assert.False(parsed.IsDynamic);
        Assert.True(parsed.Fields.ContainsKey("title"));
    }

    [Fact]
    public void Parse_Null_ReturnsNull()
    {
        Assert.Null(SearchIndexDefinition.Parse(null));
    }

    [Fact]
    public void Parse_DocWithoutMappings_ReturnsNull()
    {
        Assert.Null(SearchIndexDefinition.Parse(new BsonDocument("other", "value")));
    }
}
