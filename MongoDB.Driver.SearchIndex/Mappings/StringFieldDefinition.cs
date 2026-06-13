using MongoDB.Bson;

namespace MongoDB.Driver.SearchIndex.Mappings;

public enum SearchFieldNorms
{
    Include,
    Omit,
}

public enum SearchIndexOptions
{
    Offsets,
    Freqs,
    Positions,
}

public class StringFieldDefinition : SearchFieldDefinition
{
    public string? Analyzer { get; init; }
    public SearchIndexOptions? IndexOptions { get; init; }
    public string? SearchAnalyzer { get; init; }
    public bool? Store { get; init; }
    public SearchFieldNorms? Norms { get; init; }
    public int? IgnoreAbove { get; init; }
    public SearchSimilarityType? Similarity { get; init; }
    public Dictionary<string, SearchFieldDefinition>? Multi { get; init; }

    public override BsonDocument ToBsonDocument()
    {
        var doc = new BsonDocument("type", "string");
        if (Analyzer != null)      doc["analyzer"]      = Analyzer;
        if (IndexOptions != null)  doc["indexOptions"]  = IndexOptions.Value.ToString().ToLowerInvariant();
        if (SearchAnalyzer != null)doc["searchAnalyzer"]= SearchAnalyzer;
        if (Store != null)         doc["store"]         = Store.Value;
        if (Norms != null)         doc["norms"]         = Norms.Value.ToString().ToLowerInvariant();
        if (IgnoreAbove != null)   doc["ignoreAbove"]   = IgnoreAbove.Value;
        var similarity = SerializeSimilarity(Similarity);
        if (similarity != null) doc["similarity"] = similarity;
        var multi = SerializeMulti(Multi);
        if (multi != null) doc["multi"] = multi;
        return doc;
    }
}
