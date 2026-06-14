using MongoDB.Bson;
using MongoDB.Driver.SearchIndex.Mappings;
using Xunit;

namespace MongoDB.Driver.SearchIndex.Tests;

/// <summary>
/// Tests that each field type serializes correctly to BsonDocument.
/// </summary>
public class FieldSerializationTests
{
    private static BsonDocument GetField(SearchIndexDefinition def, string path)
    {
        var doc = def.ToBsonDocument();
        var value = doc["mappings"]["fields"][path];
        return value is BsonArray arr ? arr[0].AsBsonDocument : value.AsBsonDocument;
    }

    // ── string ────────────────────────────────────────────────────────────────

    [Fact]
    public void String_DefaultOptions()
    {
        var field = GetField(SearchIndexDefinition.Static().StringField("f"), "f");
        Assert.Equal("string", field["type"].AsString);
        Assert.False(field.Contains("analyzer"));
    }

    [Fact]
    public void String_WithAnalyzer()
    {
        var field = GetField(SearchIndexDefinition.Static().StringField("f", SearchAnalyzer.English), "f");
        Assert.Equal(SearchAnalyzer.English, field["analyzer"].AsString);
    }

    [Fact]
    public void String_WithNorms()
    {
        var field = GetField(SearchIndexDefinition.Static().StringField("f", norms: SearchFieldNorms.Omit), "f");
        Assert.Equal("omit", field["norms"].AsString);
    }

    [Fact]
    public void String_WithIndexOptions()
    {
        var field = GetField(SearchIndexDefinition.Static().StringField("f", indexOptions: SearchIndexOptions.Freqs), "f");
        Assert.Equal("freqs", field["indexOptions"].AsString);
    }

    [Fact]
    public void String_WithSimilarity()
    {
        var field = GetField(SearchIndexDefinition.Static().StringField("f", similarity: SearchSimilarityType.Boolean), "f");
        Assert.Equal("boolean", field["similarity"]["type"].AsString);
    }

    [Fact]
    public void String_WithStore()
    {
        var field = GetField(SearchIndexDefinition.Static().StringField("f", store: true), "f");
        Assert.True(field["store"].AsBoolean);
    }

    [Fact]
    public void String_WithMulti()
    {
        var def = SearchIndexDefinition.Static()
            .StringField("f", analyzer: SearchAnalyzer.English, multi: new()
            {
                ["keyword"] = new StringFieldDefinition { Analyzer = SearchAnalyzer.Keyword },
                ["ru"]      = new StringFieldDefinition { Analyzer = SearchAnalyzer.Russian },
            });
        var field = GetField(def, "f");
        Assert.Equal(SearchAnalyzer.English, field["analyzer"].AsString);
        var multi = field["multi"].AsBsonDocument;
        Assert.Equal(SearchAnalyzer.Keyword, multi["keyword"]["analyzer"].AsString);
        Assert.Equal(SearchAnalyzer.Russian, multi["ru"]["analyzer"].AsString);
    }

    // ── autocomplete ─────────────────────────────────────────────────────────

    [Fact]
    public void Autocomplete_DefaultOptions()
    {
        var field = GetField(SearchIndexDefinition.Static().AutocompleteField("f"), "f");
        Assert.Equal("autocomplete", field["type"].AsString);
        Assert.Equal("edgeGram", field["tokenization"].AsString);
        Assert.Equal(2, field["minGrams"].AsInt32);
        Assert.Equal(15, field["maxGrams"].AsInt32);
    }

    [Fact]
    public void Autocomplete_NGram()
    {
        var field = GetField(
            SearchIndexDefinition.Static().AutocompleteField("f", AutocompleteTokenization.NGram, minGrams: 3, maxGrams: 10),
            "f");
        Assert.Equal("nGram", field["tokenization"].AsString);
        Assert.Equal(3, field["minGrams"].AsInt32);
        Assert.Equal(10, field["maxGrams"].AsInt32);
    }

    [Fact]
    public void Autocomplete_RightEdgeGram()
    {
        var field = GetField(
            SearchIndexDefinition.Static().AutocompleteField("f", AutocompleteTokenization.RightEdgeGram),
            "f");
        Assert.Equal("rightEdgeGram", field["tokenization"].AsString);
    }

    [Fact]
    public void Autocomplete_FoldDiacritics_False()
    {
        var field = GetField(
            SearchIndexDefinition.Static().AutocompleteField("f", foldDiacritics: false),
            "f");
        Assert.False(field["foldDiacritics"].AsBoolean);
    }

    // ── boolean ──────────────────────────────────────────────────────────────

    [Fact]
    public void Boolean_Serializes()
    {
        var field = GetField(SearchIndexDefinition.Static().BooleanField("f"), "f");
        Assert.Equal("boolean", field["type"].AsString);
    }

    // ── date ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Date_Serializes()
    {
        var field = GetField(SearchIndexDefinition.Static().DateField("f"), "f");
        Assert.Equal("date", field["type"].AsString);
    }

    [Fact]
    public void DateFacet_Serializes()
    {
        var field = GetField(SearchIndexDefinition.Static().DateFacetField("f"), "f");
        Assert.Equal("dateFacet", field["type"].AsString);
    }

    // ── objectId / uuid ──────────────────────────────────────────────────────

    [Fact]
    public void ObjectId_Serializes()
    {
        var field = GetField(SearchIndexDefinition.Static().ObjectIdField("f"), "f");
        Assert.Equal("objectId", field["type"].AsString);
    }

    [Fact]
    public void Uuid_Serializes()
    {
        var field = GetField(SearchIndexDefinition.Static().UuidField("f"), "f");
        Assert.Equal("uuid", field["type"].AsString);
    }

    // ── token / stringFacet ──────────────────────────────────────────────────

    [Fact]
    public void Token_DefaultOptions()
    {
        var field = GetField(SearchIndexDefinition.Static().TokenField("f"), "f");
        Assert.Equal("token", field["type"].AsString);
        Assert.False(field.Contains("normalizer"));
    }

    [Fact]
    public void Token_WithLowercase()
    {
        var field = GetField(SearchIndexDefinition.Static().TokenField("f", TokenNormalizer.Lowercase), "f");
        Assert.Equal("lowercase", field["normalizer"].AsString);
    }

    [Fact]
    public void StringFacet_Serializes()
    {
        var field = GetField(SearchIndexDefinition.Static().StringFacetField("f"), "f");
        Assert.Equal("stringFacet", field["type"].AsString);
    }

    // ── number / numberFacet ─────────────────────────────────────────────────

    [Fact]
    public void Number_DefaultOptions()
    {
        var field = GetField(SearchIndexDefinition.Static().NumberField("f"), "f");
        Assert.Equal("number", field["type"].AsString);
        Assert.Equal("double", field["representation"].AsString);
        Assert.False(field.Contains("indexIntegers"));
        Assert.False(field.Contains("indexDoubles"));
    }

    [Fact]
    public void Number_Int64Representation()
    {
        var field = GetField(
            SearchIndexDefinition.Static().NumberField("f", NumberRepresentation.Int64),
            "f");
        Assert.Equal("int64", field["representation"].AsString);
    }

    [Fact]
    public void Number_IndexIntegersFalse()
    {
        var field = GetField(
            SearchIndexDefinition.Static().NumberField("f", indexIntegers: false),
            "f");
        Assert.False(field["indexIntegers"].AsBoolean);
    }

    [Fact]
    public void NumberFacet_DefaultOptions()
    {
        var field = GetField(SearchIndexDefinition.Static().NumberFacetField("f"), "f");
        Assert.Equal("numberFacet", field["type"].AsString);
        Assert.Equal("double", field["representation"].AsString);
    }

    // ── geo ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Geo_DefaultOptions()
    {
        var field = GetField(SearchIndexDefinition.Static().GeoField("f"), "f");
        Assert.Equal("geo", field["type"].AsString);
        Assert.False(field.Contains("indexShapes"));
    }

    [Fact]
    public void Geo_WithIndexShapes()
    {
        var field = GetField(SearchIndexDefinition.Static().GeoField("f", indexShapes: true), "f");
        Assert.True(field["indexShapes"].AsBoolean);
    }

    // ── vector ───────────────────────────────────────────────────────────────

    [Fact]
    public void Vector_RequiredOnly()
    {
        var field = GetField(
            SearchIndexDefinition.Static().VectorField("f", 1536, KnnVectorSimilarity.Cosine),
            "f");
        Assert.Equal("vector", field["type"].AsString);
        Assert.Equal(1536, field["numDimensions"].AsInt32);
        Assert.Equal("cosine", field["similarity"].AsString);
        Assert.False(field.Contains("quantization"));
        Assert.False(field.Contains("indexingMethod"));
    }

    [Fact]
    public void Vector_DotProduct()
    {
        var field = GetField(
            SearchIndexDefinition.Static().VectorField("f", 768, KnnVectorSimilarity.DotProduct),
            "f");
        Assert.Equal("dotProduct", field["similarity"].AsString);
    }

    [Fact]
    public void Vector_Euclidean()
    {
        var field = GetField(
            SearchIndexDefinition.Static().VectorField("f", 128, KnnVectorSimilarity.Euclidean),
            "f");
        Assert.Equal("euclidean", field["similarity"].AsString);
    }

    [Fact]
    public void Vector_WithScalarQuantization()
    {
        var field = GetField(
            SearchIndexDefinition.Static().VectorField("f", 1536, KnnVectorSimilarity.Cosine,
                quantization: VectorQuantization.Scalar),
            "f");
        Assert.Equal("scalar", field["quantization"].AsString);
    }

    [Fact]
    public void Vector_WithFlatIndexing()
    {
        var field = GetField(
            SearchIndexDefinition.Static().VectorField("f", 1536, KnnVectorSimilarity.Cosine,
                indexingMethod: VectorIndexingMethod.Flat),
            "f");
        Assert.Equal("flat", field["indexingMethod"].AsString);
        Assert.False(field.Contains("hnswOptions"));
    }

    [Fact]
    public void Vector_WithHnswOptions()
    {
        var field = GetField(
            SearchIndexDefinition.Static().VectorField("f", 1536, KnnVectorSimilarity.Cosine,
                hnswOptions: new HnswOptions { MaxEdges = 32, NumEdgeCandidates = 200 }),
            "f");
        Assert.Equal(32, field["hnswOptions"]["maxEdges"].AsInt32);
        Assert.Equal(200, field["hnswOptions"]["numEdgeCandidates"].AsInt32);
    }

    [Fact]
    public void Vector_HnswOptions_NotSerializedWhenFlat()
    {
        var field = GetField(
            SearchIndexDefinition.Static().VectorField("f", 1536, KnnVectorSimilarity.Cosine,
                indexingMethod: VectorIndexingMethod.Flat,
                hnswOptions: new HnswOptions { MaxEdges = 32 }),
            "f");
        Assert.False(field.Contains("hnswOptions"));
    }

    // ── knnVector (deprecated) ────────────────────────────────────────────────

    [Fact]
    public void KnnVector_Serializes()
    {
        var field = GetField(
            SearchIndexDefinition.Static().KnnVectorField("f", 128, KnnVectorSimilarity.Euclidean),
            "f");
        Assert.Equal("knnVector", field["type"].AsString);
        Assert.Equal(128, field["dimensions"].AsInt32);
        Assert.Equal("euclidean", field["similarity"].AsString);
    }

    // ── document ─────────────────────────────────────────────────────────────

    [Fact]
    public void Document_Dynamic()
    {
        var field = GetField(
            SearchIndexDefinition.Static().DocumentField("f", nested => { }),
            "f");
        Assert.Equal("document", field["type"].AsString);
        Assert.False(field["dynamic"].AsBoolean);
    }

    [Fact]
    public void Document_WithNestedFields()
    {
        var field = GetField(
            SearchIndexDefinition.Static()
                .DocumentField("address", nested => nested
                    .StringField("city")
                    .StringField("country")),
            "address");
        Assert.True(field["fields"].AsBsonDocument.Contains("city"));
        Assert.True(field["fields"].AsBsonDocument.Contains("country"));
    }

    // ── embeddedDocuments ─────────────────────────────────────────────────────

    [Fact]
    public void EmbeddedDocuments_Serializes()
    {
        var field = GetField(
            SearchIndexDefinition.Static()
                .EmbeddedDocumentsField("reviews", nested => nested.StringField("text")),
            "reviews");
        Assert.Equal("embeddedDocuments", field["type"].AsString);
        Assert.True(field["fields"].AsBsonDocument.Contains("text"));
    }

    // ── array ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Array_WithStringElement()
    {
        var field = GetField(
            SearchIndexDefinition.Static()
                .ArrayField("tags", new StringFieldDefinition()),
            "tags");
        Assert.Equal("string", field["type"].AsString);
    }

    [Fact]
    public void Array_WithNumberElement()
    {
        var field = GetField(
            SearchIndexDefinition.Static()
                .ArrayField("scores", new NumberFieldDefinition()),
            "scores");
        Assert.Equal("number", field["type"].AsString);
    }
}

