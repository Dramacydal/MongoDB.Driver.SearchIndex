using MongoDB.Driver.SearchIndex.Mappings;
using Xunit;

namespace MongoDB.Driver.SearchIndex.Tests;

/// <summary>
/// Round-trip tests: serialize to BsonDocument then parse back and verify field types and properties.
/// </summary>
public class FieldParseTests
{
    private static T RoundTrip<T>(SearchIndexDefinition def, string path) where T : SearchFieldDefinition
    {
        var parsed = SearchIndexDefinition.Parse(def.ToBsonDocument())!;
        var field = parsed.Fields[path].OfType<T>().FirstOrDefault();
        Assert.NotNull(field);
        return field;
    }

    // ── string ────────────────────────────────────────────────────────────────

    [Fact]
    public void String_DefaultOptions_RoundTrip()
    {
        var field = RoundTrip<StringFieldDefinition>(
            SearchIndexDefinition.Static().StringField("f"), "f");
        Assert.Null(field.Analyzer);
        Assert.Null(field.Norms);
    }

    [Fact]
    public void String_Analyzer_RoundTrip()
    {
        var field = RoundTrip<StringFieldDefinition>(
            SearchIndexDefinition.Static().StringField("f", SearchAnalyzer.English), "f");
        Assert.Equal(SearchAnalyzer.English, field.Analyzer);
    }

    [Fact]
    public void String_AllOptions_RoundTrip()
    {
        var original = SearchIndexDefinition.Static()
            .ArrayField("f", new StringFieldDefinition
            {
                Analyzer     = SearchAnalyzer.Russian,
                IndexOptions = SearchIndexOptions.Offsets,
                Norms        = SearchFieldNorms.Omit,
                Similarity   = SearchSimilarityType.Bm25,
                Store        = true,
                IgnoreAbove  = 256,
            });

        var field = RoundTrip<StringFieldDefinition>(original, "f");
        Assert.Equal(SearchAnalyzer.Russian, field.Analyzer);
        Assert.Equal(SearchIndexOptions.Offsets, field.IndexOptions);
        Assert.Equal(SearchFieldNorms.Omit, field.Norms);
        Assert.Equal(SearchSimilarityType.Bm25, field.Similarity);
        Assert.True(field.Store);
        Assert.Equal(256, field.IgnoreAbove);
    }

    // ── autocomplete ─────────────────────────────────────────────────────────

    [Fact]
    public void Autocomplete_DefaultOptions_RoundTrip()
    {
        var field = RoundTrip<AutocompleteFieldDefinition>(
            SearchIndexDefinition.Static().AutocompleteField("f"), "f");
        Assert.Equal(AutocompleteTokenization.EdgeGram, field.Tokenization);
        Assert.Equal(2, field.MinGrams);
        Assert.Equal(15, field.MaxGrams);
        Assert.True(field.FoldDiacritics);
    }

    [Fact]
    public void Autocomplete_CustomOptions_RoundTrip()
    {
        var field = RoundTrip<AutocompleteFieldDefinition>(
            SearchIndexDefinition.Static().AutocompleteField("f",
                AutocompleteTokenization.NGram, minGrams: 3, maxGrams: 8,
                foldDiacritics: false, analyzer: SearchAnalyzer.French),
            "f");
        Assert.Equal(AutocompleteTokenization.NGram, field.Tokenization);
        Assert.Equal(3, field.MinGrams);
        Assert.Equal(8, field.MaxGrams);
        Assert.False(field.FoldDiacritics);
        Assert.Equal(SearchAnalyzer.French, field.Analyzer);
    }

    // ── boolean ──────────────────────────────────────────────────────────────

    [Fact]
    public void Boolean_RoundTrip()
    {
        RoundTrip<BooleanFieldDefinition>(
            SearchIndexDefinition.Static().BooleanField("f"), "f");
    }

    // ── date ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Date_RoundTrip()
    {
        RoundTrip<DateFieldDefinition>(
            SearchIndexDefinition.Static().DateField("f"), "f");
    }

    [Fact]
    public void DateFacet_RoundTrip()
    {
        RoundTrip<DateFacetFieldDefinition>(
            SearchIndexDefinition.Static().DateFacetField("f"), "f");
    }

    // ── objectId / uuid ──────────────────────────────────────────────────────

    [Fact]
    public void ObjectId_RoundTrip()
    {
        RoundTrip<ObjectIdFieldDefinition>(
            SearchIndexDefinition.Static().ObjectIdField("f"), "f");
    }

    [Fact]
    public void Uuid_RoundTrip()
    {
        RoundTrip<UuidFieldDefinition>(
            SearchIndexDefinition.Static().UuidField("f"), "f");
    }

    // ── token / stringFacet ──────────────────────────────────────────────────

    [Fact]
    public void Token_None_RoundTrip()
    {
        var field = RoundTrip<TokenFieldDefinition>(
            SearchIndexDefinition.Static().TokenField("f"), "f");
        Assert.Equal(TokenNormalizer.None, field.Normalizer);
    }

    [Fact]
    public void Token_Lowercase_RoundTrip()
    {
        var field = RoundTrip<TokenFieldDefinition>(
            SearchIndexDefinition.Static().TokenField("f", TokenNormalizer.Lowercase), "f");
        Assert.Equal(TokenNormalizer.Lowercase, field.Normalizer);
    }

    [Fact]
    public void StringFacet_RoundTrip()
    {
        RoundTrip<StringFacetFieldDefinition>(
            SearchIndexDefinition.Static().StringFacetField("f"), "f");
    }

    // ── number / numberFacet ─────────────────────────────────────────────────

    [Fact]
    public void Number_DefaultOptions_RoundTrip()
    {
        var field = RoundTrip<NumberFieldDefinition>(
            SearchIndexDefinition.Static().NumberField("f"), "f");
        Assert.Equal(NumberRepresentation.Double, field.Representation);
        Assert.True(field.IndexIntegers);
        Assert.True(field.IndexDoubles);
    }

    [Fact]
    public void Number_Int64_RoundTrip()
    {
        var field = RoundTrip<NumberFieldDefinition>(
            SearchIndexDefinition.Static().NumberField("f", NumberRepresentation.Int64), "f");
        Assert.Equal(NumberRepresentation.Int64, field.Representation);
    }

    [Fact]
    public void Number_IndexFlagsOff_RoundTrip()
    {
        var field = RoundTrip<NumberFieldDefinition>(
            SearchIndexDefinition.Static().NumberField("f", indexIntegers: false, indexDoubles: false), "f");
        Assert.False(field.IndexIntegers);
        Assert.False(field.IndexDoubles);
    }

    [Fact]
    public void NumberFacet_DefaultOptions_RoundTrip()
    {
        var field = RoundTrip<NumberFacetFieldDefinition>(
            SearchIndexDefinition.Static().NumberFacetField("f"), "f");
        Assert.Equal(NumberRepresentation.Double, field.Representation);
    }

    [Fact]
    public void NumberFacet_Int64_RoundTrip()
    {
        var field = RoundTrip<NumberFacetFieldDefinition>(
            SearchIndexDefinition.Static().NumberFacetField("f", NumberRepresentation.Int64), "f");
        Assert.Equal(NumberRepresentation.Int64, field.Representation);
    }

    // ── geo ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Geo_DefaultOptions_RoundTrip()
    {
        var field = RoundTrip<GeoFieldDefinition>(
            SearchIndexDefinition.Static().GeoField("f"), "f");
        Assert.False(field.IndexShapes);
    }

    [Fact]
    public void Geo_WithIndexShapes_RoundTrip()
    {
        var field = RoundTrip<GeoFieldDefinition>(
            SearchIndexDefinition.Static().GeoField("f", indexShapes: true), "f");
        Assert.True(field.IndexShapes);
    }

    // ── vector ───────────────────────────────────────────────────────────────

    [Fact]
    public void Vector_RequiredOnly_RoundTrip()
    {
        var field = RoundTrip<VectorFieldDefinition>(
            SearchIndexDefinition.Static().VectorField("f", 1536, KnnVectorSimilarity.Cosine), "f");
        Assert.Equal(1536, field.NumDimensions);
        Assert.Equal(KnnVectorSimilarity.Cosine, field.Similarity);
        Assert.Equal(VectorQuantization.None, field.Quantization);
        Assert.Equal(VectorIndexingMethod.Hnsw, field.IndexingMethod);
    }

    [Fact]
    public void Vector_AllSimilarities_RoundTrip()
    {
        foreach (var sim in new[] { KnnVectorSimilarity.Euclidean, KnnVectorSimilarity.Cosine, KnnVectorSimilarity.DotProduct })
        {
            var field = RoundTrip<VectorFieldDefinition>(
                SearchIndexDefinition.Static().VectorField("f", 128, sim), "f");
            Assert.Equal(sim, field.Similarity);
        }
    }

    [Fact]
    public void Vector_WithScalarQuantization_RoundTrip()
    {
        var field = RoundTrip<VectorFieldDefinition>(
            SearchIndexDefinition.Static().VectorField("f", 1536, KnnVectorSimilarity.Cosine,
                quantization: VectorQuantization.Scalar),
            "f");
        Assert.Equal(VectorQuantization.Scalar, field.Quantization);
    }

    [Fact]
    public void Vector_WithBinaryQuantization_RoundTrip()
    {
        var field = RoundTrip<VectorFieldDefinition>(
            SearchIndexDefinition.Static().VectorField("f", 1536, KnnVectorSimilarity.Cosine,
                quantization: VectorQuantization.Binary),
            "f");
        Assert.Equal(VectorQuantization.Binary, field.Quantization);
    }

    [Fact]
    public void Vector_WithFlatIndexing_RoundTrip()
    {
        var field = RoundTrip<VectorFieldDefinition>(
            SearchIndexDefinition.Static().VectorField("f", 1536, KnnVectorSimilarity.Cosine,
                indexingMethod: VectorIndexingMethod.Flat),
            "f");
        Assert.Equal(VectorIndexingMethod.Flat, field.IndexingMethod);
    }

    [Fact]
    public void Vector_WithHnswOptions_RoundTrip()
    {
        var field = RoundTrip<VectorFieldDefinition>(
            SearchIndexDefinition.Static().VectorField("f", 1536, KnnVectorSimilarity.Cosine,
                hnswOptions: new HnswOptions { MaxEdges = 32, NumEdgeCandidates = 200 }),
            "f");
        Assert.NotNull(field.HnswOptions);
        Assert.Equal(32, field.HnswOptions.MaxEdges);
        Assert.Equal(200, field.HnswOptions.NumEdgeCandidates);
    }

    // ── knnVector (deprecated) ────────────────────────────────────────────────

    [Fact]
    public void KnnVector_RoundTrip()
    {
        var field = RoundTrip<KnnVectorFieldDefinition>(
            SearchIndexDefinition.Static().KnnVectorField("f", 128, KnnVectorSimilarity.DotProduct), "f");
        Assert.Equal(128, field.Dimensions);
        Assert.Equal(KnnVectorSimilarity.DotProduct, field.Similarity);
    }

    // ── document ─────────────────────────────────────────────────────────────

    [Fact]
    public void Document_Static_RoundTrip()
    {
        var field = RoundTrip<DocumentFieldDefinition>(
            SearchIndexDefinition.Static()
                .DocumentField("addr", nested => nested.StringField("city").NumberField("zip")),
            "addr");
        Assert.False(field.Dynamic);
        Assert.NotNull(field.Fields);
        Assert.True(field.Fields.ContainsKey("city"));
        Assert.True(field.Fields.ContainsKey("zip"));
    }

    [Fact]
    public void Document_Dynamic_ParsedFromBson()
    {
        var doc = new MongoDB.Bson.BsonDocument("mappings", new MongoDB.Bson.BsonDocument
        {
            { "dynamic", false },
            { "fields", new MongoDB.Bson.BsonDocument("addr", new MongoDB.Bson.BsonDocument
                { { "type", "document" }, { "dynamic", true } }) },
        });
        var parsed = SearchIndexDefinition.Parse(doc)!;
        var field = parsed.Fields["addr"].OfType<DocumentFieldDefinition>().First();
        Assert.True(field.Dynamic);
    }

    // ── embeddedDocuments ─────────────────────────────────────────────────────

    [Fact]
    public void EmbeddedDocuments_Static_RoundTrip()
    {
        var field = RoundTrip<EmbeddedDocumentsFieldDefinition>(
            SearchIndexDefinition.Static()
                .EmbeddedDocumentsField("reviews", nested => nested
                    .StringField("author")
                    .StringField("text")),
            "reviews");
        Assert.False(field.Dynamic);
        Assert.NotNull(field.Fields);
        Assert.True(field.Fields.ContainsKey("author"));
        Assert.True(field.Fields.ContainsKey("text"));
    }

    [Fact]
    public void EmbeddedDocuments_Dynamic_ParsedFromBson()
    {
        var doc = new MongoDB.Bson.BsonDocument("mappings", new MongoDB.Bson.BsonDocument
        {
            { "dynamic", false },
            { "fields", new MongoDB.Bson.BsonDocument("items", new MongoDB.Bson.BsonDocument
                { { "type", "embeddedDocuments" }, { "dynamic", true } }) },
        });
        var parsed = SearchIndexDefinition.Parse(doc)!;
        var field = parsed.Fields["items"].OfType<EmbeddedDocumentsFieldDefinition>().First();
        Assert.True(field.Dynamic);
    }

    // ── array ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Array_StringElement_RoundTrip()
    {
        var field = RoundTrip<StringFieldDefinition>(
            SearchIndexDefinition.Static()
                .ArrayField("tags", new StringFieldDefinition { Analyzer = SearchAnalyzer.English }),
            "tags");
        Assert.Equal(SearchAnalyzer.English, field.Analyzer);
    }

    [Fact]
    public void Array_NumberElement_RoundTrip()
    {
        RoundTrip<NumberFieldDefinition>(
            SearchIndexDefinition.Static().ArrayField("scores", new NumberFieldDefinition()),
            "scores");
    }

    [Fact]
    public void Array_BooleanElement_RoundTrip()
    {
        RoundTrip<BooleanFieldDefinition>(
            SearchIndexDefinition.Static().ArrayField("flags", new BooleanFieldDefinition()),
            "flags");
    }
}
