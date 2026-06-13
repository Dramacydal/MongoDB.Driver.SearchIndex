using MongoDB.Bson;
using MongoDB.Driver.Search;
using MongoDB.Driver.SearchIndex.Mappings;
using Xunit;

namespace MongoDB.Driver.SearchIndex.Tests.Integration;

[Collection("Integration")]
public class SearchIndexIntegrationTests : IClassFixture<MongoFixture>
{
    private readonly MongoFixture _fixture;

    public SearchIndexIntegrationTests(MongoFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<IMongoCollection<BsonDocument>> NewCollection(string suffix)
    {
        var col = _fixture.GetCollection($"col_{suffix}_{Guid.NewGuid().ToString("N")[..8]}");
        // Atlas Search requires the collection to exist before creating an index
        await col.InsertOneAsync(new BsonDocument("_init", true));
        return col;
    }

    private static Task<List<BsonDocument>> Search(
        IMongoCollection<BsonDocument> collection,
        string indexName,
        SearchDefinition<BsonDocument> searchDefinition)
        => collection
            .Aggregate()
            .Search(searchDefinition, searchOptions: new SearchOptions<BsonDocument> { IndexName = indexName })
            .ToListAsync();

    // ── basic lifecycle ───────────────────────────────────────────────────────

    [RequiresMongo]
    public async Task CreateOne_ReturnsNonEmptyId()
    {
        var col = await NewCollection("lifecycle");

        var id = await col.SearchIndexes.CreateOneAsync("idx",
            SearchIndexDefinition.Static().StringField("title"));

        Assert.NotEmpty(id);
    }

    [RequiresMongo]
    public async Task GetSearchIndexes_ReturnsCreatedIndex()
    {
        var col = await NewCollection("listing");

        await col.SearchIndexes.CreateOneAsync("my_index",
            SearchIndexDefinition.Static().StringField("body"));

        await MongoFixture.WaitForIndexReady(col, "my_index");

        var indexes = await col.SearchIndexes.GetSearchIndexesAsync();
        Assert.Contains(indexes, x => x.Name == "my_index");
    }

    [RequiresMongo]
    public async Task GetSearchIndexes_ByName_ReturnsOnlyThatIndex()
    {
        var col = await NewCollection("byname");

        await col.SearchIndexes.CreateOneAsync("idx_a",
            SearchIndexDefinition.Static().StringField("a"));
        await col.SearchIndexes.CreateOneAsync("idx_b",
            SearchIndexDefinition.Static().StringField("b"));

        await MongoFixture.WaitForIndexReady(col, "idx_a");
        await MongoFixture.WaitForIndexReady(col, "idx_b");

        var result = await col.SearchIndexes.GetSearchIndexesAsync("idx_a");
        Assert.Single(result);
        Assert.Equal("idx_a", result[0].Name);
    }

    [RequiresMongo]
    public async Task IndexInfo_StatusAndQueryable_AreCorrect()
    {
        var col = await NewCollection("status");

        await col.SearchIndexes.CreateOneAsync("idx",
            SearchIndexDefinition.Static().StringField("title"));

        await MongoFixture.WaitForIndexReady(col, "idx");

        var indexes = await col.SearchIndexes.GetSearchIndexesAsync("idx");
        var idx = Assert.Single(indexes);
        Assert.Equal(SearchIndexStatus.Ready, idx.Status);
        Assert.True(idx.Queryable);
        Assert.Equal(SearchIndexType.Search, idx.Type);
    }

    // ── string field ─────────────────────────────────────────────────────────

    [RequiresMongo]
    public async Task StringField_LatestDefinition_ParsedCorrectly()
    {
        var col = await NewCollection("string");

        await col.SearchIndexes.CreateOneAsync("idx",
            SearchIndexDefinition.Static()
                .StringField("title", SearchAnalyzer.English)
                .StringField("body",  SearchAnalyzer.Standard));

        await MongoFixture.WaitForIndexReady(col, "idx");

        var idx = (await col.SearchIndexes.GetSearchIndexesAsync("idx"))[0];
        var def = idx.LatestDefinition;
        Assert.NotNull(def);
        Assert.False(def.IsDynamic);

        var title = def.Fields["title"].OfType<StringFieldDefinition>().First();
        Assert.Equal(SearchAnalyzer.English, title.Analyzer);

        var body = def.Fields["body"].OfType<StringFieldDefinition>().First();
        Assert.Equal(SearchAnalyzer.Standard, body.Analyzer);
    }

    [RequiresMongo]
    public async Task StringField_Search_ReturnsMatchingDocuments()
    {
        var col = await NewCollection("strsearch");

        await col.InsertManyAsync([
            new BsonDocument { { "title", "Atlas Search is powerful" } },
            new BsonDocument { { "title", "MongoDB aggregation pipelines" } },
            new BsonDocument { { "title", "Atlas Vector Search embeddings" } },
        ]);

        await col.SearchIndexes.CreateOneAsync("idx",
            SearchIndexDefinition.Static().StringField("title", SearchAnalyzer.English));

        await MongoFixture.WaitForIndexReady(col, "idx");

        var results = await Search(col, "idx",
            Builders<BsonDocument>.Search.Text("title", "atlas"));

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Contains("Atlas", r["title"].AsString));
    }

    // ── autocomplete field ────────────────────────────────────────────────────

    [RequiresMongo]
    public async Task AutocompleteField_LatestDefinition_ParsedCorrectly()
    {
        var col = await NewCollection("autocomplete");

        await col.SearchIndexes.CreateOneAsync("idx",
            SearchIndexDefinition.Static()
                .AutocompleteField("name", AutocompleteTokenization.EdgeGram,
                    minGrams: 3, maxGrams: 10));

        await MongoFixture.WaitForIndexReady(col, "idx");

        var def = (await col.SearchIndexes.GetSearchIndexesAsync("idx"))[0].LatestDefinition;
        Assert.NotNull(def);

        var field = def.Fields["name"].OfType<AutocompleteFieldDefinition>().First();
        Assert.Equal(AutocompleteTokenization.EdgeGram, field.Tokenization);
        Assert.Equal(3, field.MinGrams);
        Assert.Equal(10, field.MaxGrams);
    }

    [RequiresMongo]
    public async Task AutocompleteField_Search_ReturnsPartialMatch()
    {
        var col = await NewCollection("autosearch");

        await col.InsertManyAsync([
            new BsonDocument("name", "William"),
            new BsonDocument("name", "George"),
            new BsonDocument("name", "Philip"),
        ]);

        await col.SearchIndexes.CreateOneAsync("idx",
            SearchIndexDefinition.Static().AutocompleteField("name"));

        await MongoFixture.WaitForIndexReady(col, "idx");

        var results = await Search(col, "idx",
            Builders<BsonDocument>.Search.Autocomplete("name", "Wil"));

        Assert.Single(results);
        Assert.Equal("William", results[0]["name"].AsString);
    }

    // ── dynamic mapping ───────────────────────────────────────────────────────

    [RequiresMongo]
    public async Task DynamicIndex_LatestDefinition_IsDynamic()
    {
        var col = await NewCollection("dynamic");

        await col.SearchIndexes.CreateOneAsync("idx", SearchIndexDefinition.Dynamic());
        await MongoFixture.WaitForIndexReady(col, "idx");

        var def = (await col.SearchIndexes.GetSearchIndexesAsync("idx"))[0].LatestDefinition;
        Assert.NotNull(def);
        Assert.True(def.IsDynamic);
    }

    // ── number field ──────────────────────────────────────────────────────────

    [RequiresMongo]
    public async Task NumberField_LatestDefinition_ParsedCorrectly()
    {
        var col = await NewCollection("number");

        await col.SearchIndexes.CreateOneAsync("idx",
            SearchIndexDefinition.Static()
                .NumberField("price",    NumberRepresentation.Double)
                .NumberField("quantity", NumberRepresentation.Int64));

        await MongoFixture.WaitForIndexReady(col, "idx");

        var def = (await col.SearchIndexes.GetSearchIndexesAsync("idx"))[0].LatestDefinition;
        Assert.NotNull(def);

        var price = def.Fields["price"].OfType<NumberFieldDefinition>().First();
        Assert.Equal(NumberRepresentation.Double, price.Representation);

        var qty = def.Fields["quantity"].OfType<NumberFieldDefinition>().First();
        Assert.Equal(NumberRepresentation.Int64, qty.Representation);
    }

    [RequiresMongo]
    public async Task NumberField_RangeSearch_ReturnsMatchingDocuments()
    {
        var col = await NewCollection("numrange");

        await col.InsertManyAsync([
            new BsonDocument("price", 10.0),
            new BsonDocument("price", 25.0),
            new BsonDocument("price", 50.0),
            new BsonDocument("price", 99.0),
        ]);

        await col.SearchIndexes.CreateOneAsync("idx",
            SearchIndexDefinition.Static().NumberField("price"));

        await MongoFixture.WaitForIndexReady(col, "idx");

        var results = await Search(col, "idx",
            Builders<BsonDocument>.Search.Range("price", SearchRangeBuilder.Gte(20.0).Lte(60.0)));

        Assert.Equal(2, results.Count);
    }

    // ── boolean field ─────────────────────────────────────────────────────────

    [RequiresMongo]
    public async Task BooleanField_Search_ReturnsMatchingDocuments()
    {
        var col = await NewCollection("boolean");

        await col.InsertManyAsync([
            new BsonDocument("active", true),
            new BsonDocument("active", true),
            new BsonDocument("active", false),
        ]);

        await col.SearchIndexes.CreateOneAsync("idx",
            SearchIndexDefinition.Static().BooleanField("active"));

        await MongoFixture.WaitForIndexReady(col, "idx");

        var results = await Search(col, "idx",
            Builders<BsonDocument>.Search.Equals("active", true));

        Assert.Equal(2, results.Count);
    }

    // ── date field ────────────────────────────────────────────────────────────

    [RequiresMongo]
    public async Task DateField_RangeSearch_ReturnsMatchingDocuments()
    {
        var col = await NewCollection("date");
        var now = DateTime.UtcNow;

        await col.InsertManyAsync([
            new BsonDocument("created", now.AddDays(-10)),
            new BsonDocument("created", now.AddDays(-3)),
            new BsonDocument("created", now.AddDays(-1)),
        ]);

        await col.SearchIndexes.CreateOneAsync("idx",
            SearchIndexDefinition.Static().DateField("created"));

        await MongoFixture.WaitForIndexReady(col, "idx");

        var results = await Search(col, "idx",
            Builders<BsonDocument>.Search.Range("created", SearchRangeBuilder.Gte(now.AddDays(-5))));

        Assert.Equal(2, results.Count);
    }

    // ── geo field ─────────────────────────────────────────────────────────────

    [RequiresMongo]
    public async Task GeoField_LatestDefinition_ParsedCorrectly()
    {
        var col = await NewCollection("geo");

        await col.SearchIndexes.CreateOneAsync("idx",
            SearchIndexDefinition.Static().GeoField("location", indexShapes: true));

        await MongoFixture.WaitForIndexReady(col, "idx");

        var def = (await col.SearchIndexes.GetSearchIndexesAsync("idx"))[0].LatestDefinition;
        Assert.NotNull(def);

        var field = def.Fields["location"].OfType<GeoFieldDefinition>().First();
        Assert.True(field.IndexShapes);
    }

    // ── token field ───────────────────────────────────────────────────────────

    [RequiresMongo]
    public async Task TokenField_LatestDefinition_ParsedCorrectly()
    {
        var col = await NewCollection("token");

        await col.SearchIndexes.CreateOneAsync("idx",
            SearchIndexDefinition.Static()
                .TokenField("category", TokenNormalizer.Lowercase));

        await MongoFixture.WaitForIndexReady(col, "idx");

        var def = (await col.SearchIndexes.GetSearchIndexesAsync("idx"))[0].LatestDefinition;
        Assert.NotNull(def);

        var field = def.Fields["category"].OfType<TokenFieldDefinition>().First();
        Assert.Equal(TokenNormalizer.Lowercase, field.Normalizer);
    }

    // ── document field ────────────────────────────────────────────────────────

    [RequiresMongo]
    public async Task DocumentField_NestedSearch_ReturnsMatchingDocuments()
    {
        var col = await NewCollection("document");

        await col.InsertManyAsync([
            new BsonDocument("address", new BsonDocument { { "city", "London" },     { "country", "UK" } }),
            new BsonDocument("address", new BsonDocument { { "city", "Berlin" },     { "country", "DE" } }),
            new BsonDocument("address", new BsonDocument { { "city", "Manchester" }, { "country", "UK" } }),
        ]);

        await col.SearchIndexes.CreateOneAsync("idx",
            SearchIndexDefinition.Static()
                .DocumentField("address", nested => nested
                    .StringField("city")
                    .StringField("country")));

        await MongoFixture.WaitForIndexReady(col, "idx");

        var results = await Search(col, "idx",
            Builders<BsonDocument>.Search.Text("address.city", "London"));

        Assert.Single(results);
        Assert.Equal("London", results[0]["address"]["city"].AsString);
    }

    // ── vector field ──────────────────────────────────────────────────────────

    [RequiresMongo]
    public async Task VectorField_LatestDefinition_ParsedCorrectly()
    {
        var col = await NewCollection("vector");

        await col.SearchIndexes.CreateOneAsync("idx",
            SearchIndexDefinition.Static()
                .VectorField("embedding", numDimensions: 4,
                    similarity: KnnVectorSimilarity.Cosine));

        await MongoFixture.WaitForIndexReady(col, "idx");

        var def = (await col.SearchIndexes.GetSearchIndexesAsync("idx"))[0].LatestDefinition;
        Assert.NotNull(def);

        var field = def.Fields["embedding"].OfType<VectorFieldDefinition>().First();
        Assert.Equal(4, field.NumDimensions);
        Assert.Equal(KnnVectorSimilarity.Cosine, field.Similarity);
    }

    [RequiresMongo]
    public async Task VectorField_VectorSearch_ReturnsNearestNeighbour()
    {
        var col = await NewCollection("vecsearch");

        await col.InsertManyAsync([
            new BsonDocument { { "label", "A" }, { "vec", new BsonArray { 1.0, 0.0, 0.0, 0.0 } } },
            new BsonDocument { { "label", "B" }, { "vec", new BsonArray { 0.0, 1.0, 0.0, 0.0 } } },
            new BsonDocument { { "label", "C" }, { "vec", new BsonArray { 0.0, 0.0, 1.0, 0.0 } } },
        ]);

        await col.SearchIndexes.CreateOneAsync("idx",
            SearchIndexDefinition.Static()
                .VectorField("vec", numDimensions: 4, similarity: KnnVectorSimilarity.Cosine));

        await MongoFixture.WaitForIndexReady(col, "idx");

        var results = await col.Aggregate()
            .AppendStage(PipelineStageDefinitionBuilder.VectorSearch<BsonDocument>(
                "vec",
                new float[] { 1.0f, 0.0f, 0.0f, 0.0f },
                limit: 1,
                options: new VectorSearchOptions<BsonDocument>
                {
                    IndexName = "idx",
                    NumberOfCandidates = 10,
                }))
            .ToListAsync();

        Assert.Single(results);
        Assert.Equal("A", results[0]["label"].AsString);
    }

    // ── update index ─────────────────────────────────────────────────────────

    [RequiresMongo]
    public async Task UpdateOne_ChangesIndexDefinition()
    {
        var col = await NewCollection("update");

        await col.InsertManyAsync([
            new BsonDocument { { "title", "Hello world" }, { "body", "Some content" } },
        ]);

        await col.SearchIndexes.CreateOneAsync("idx",
            SearchIndexDefinition.Static().StringField("title"));

        await MongoFixture.WaitForIndexReady(col, "idx");

        await col.SearchIndexes.UpdateOneAsync("idx",
            SearchIndexDefinition.Static()
                .StringField("title")
                .StringField("body", SearchAnalyzer.English));

        await MongoFixture.WaitForIndexReady(col, "idx");

        var def = (await col.SearchIndexes.GetSearchIndexesAsync("idx"))[0].LatestDefinition;
        Assert.NotNull(def);
        Assert.True(def.Fields.ContainsKey("body"));

        var body = def.Fields["body"].OfType<StringFieldDefinition>().First();
        Assert.Equal(SearchAnalyzer.English, body.Analyzer);
    }

    // ── multiple field types on same path ─────────────────────────────────────

    [RequiresMongo]
    public async Task MultipleFieldTypesOnPath_SearchAndAutocomplete()
    {
        var col = await NewCollection("multi");

        await col.InsertManyAsync([
            new BsonDocument("name", "Albert Einstein"),
            new BsonDocument("name", "Isaac Newton"),
            new BsonDocument("name", "Albert Camus"),
        ]);

        await col.SearchIndexes.CreateOneAsync("idx",
            SearchIndexDefinition.Static()
                .StringField("name", SearchAnalyzer.Standard)
                .AutocompleteField("name"));

        await MongoFixture.WaitForIndexReady(col, "idx");

        var textResults = await Search(col, "idx",
            Builders<BsonDocument>.Search.Text("name", "Newton"));
        Assert.Single(textResults);

        var autocompleteResults = await Search(col, "idx",
            Builders<BsonDocument>.Search.Autocomplete("name", "Alb"));
        Assert.Equal(2, autocompleteResults.Count);
    }
}
